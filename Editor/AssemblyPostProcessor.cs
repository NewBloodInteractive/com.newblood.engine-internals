#if UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Dia2Lib;
using Process = System.Diagnostics.Process;

namespace NewBlood
{
    internal static class AssemblyPostProcessor
    {
        private const string InitializedProperty = "NewBlood.EngineInternals.Initialized";

        public static string BuildVariantExecutable { get; set; }

        private static readonly List<string> s_AssemblyPaths = new List<string>();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            if (!SessionState.GetBool(InitializedProperty, false))
            {
                SessionState.SetBool(InitializedProperty, true);
            #if UNITY_2021_1_OR_NEWER
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            #elif UNITY_2019_3_OR_NEWER
                CompilationPipeline.RequestScriptCompilation();
            #elif UNITY_2017_1_OR_NEWER
                Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor", true)
                    .GetMethod("DirtyAllScripts", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, null);
            #endif
            }
        }

        private static void OnCompilationStarted(object context)
        {
            s_AssemblyPaths.Clear();
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            s_AssemblyPaths.Add(assemblyPath);
        }

        public static void ProcessAssemblies(string executable)
        {
            bool modified = false;

            IDiaSession session;
            var source = Dia2.CreateDataSource();
            source.loadDataForExe(executable, null, null);
            source.openSession(out session);
            var globalScope = session.globalScope;

            var methods    = new List<MethodDefinition>();
            var attributes = new Dictionary<MethodDefinition, CustomAttribute>();

            try
            {
                foreach (string assemblyPath in s_AssemblyPaths)
                {
                    using (var resolver = new DefaultAssemblyResolver())
                    {
                        foreach (string path in CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All))
                            resolver.AddSearchDirectory(Path.GetDirectoryName(path));

                        var parameters = new ReaderParameters
                        {
                            ReadWrite        = true,
                            AssemblyResolver = resolver,
                            ReadSymbols      = false
                        };

                        using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, parameters))
                        {
                            methods.Clear();
                            attributes.Clear();
                    
                            foreach (TypeDefinition type in assembly.MainModule.Types)
                            {
                                foreach (MethodDefinition method in type.Methods)
                                {
                                    foreach (CustomAttribute attribute in method.CustomAttributes)
                                    {
                                        if (attribute.AttributeType.FullName != typeof(SymbolImportAttribute).FullName)
                                            continue;

                                        if (!method.IsStatic)
                                        {
                                            Debug.LogErrorFormat("[EngineImport] Method must be static: {0}", method.FullName);
                                            continue;
                                        }

                                        if (method.RVA != 0)
                                            Debug.LogWarningFormat("[EngineImport] Method is not marked as extern: {0}", method.FullName);

                                        methods.Add(method);
                                        attributes.Add(method, attribute);
                                        break;
                                    }
                                }
                            }

                            if (methods.Count == 0)
                                continue;

                            for (int i = 0; i < methods.Count; i++)
                            {
                                var method     = methods[i];
                                var attribute  = attributes[method];
                                var symbolName = attribute.ConstructorArguments[0].Value.ToString();
                                method.CustomAttributes.Remove(attribute);

                                method.Body = new MethodBody(method);
                                var il      = method.Body.GetILProcessor();

                                IDiaEnumSymbols enumerator;
                                globalScope.findChildren(SymTagEnum.SymTagPublicSymbol, symbolName, 0, out enumerator);

                                if (enumerator.count <= 0)
                                {
                                    il.Emit(OpCodes.Ldstr, $"Unable to find an entry point named '{symbolName}'.");
                                    il.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(typeof(EntryPointNotFoundException).GetConstructor(new[] { typeof(string) })));
                                    il.Emit(OpCodes.Throw);
                                    continue;
                                }

                                var callConv = CallingConvention.Winapi;
                                var symbol   = enumerator.Item(0);
                                var address  = symbol.relativeVirtualAddress;

                                if (attribute.HasProperties)
                                {
                                    foreach (var property in attribute.Properties)
                                    {
                                        if (property.Name == "CallingConvention")
                                        {
                                            callConv = (CallingConvention)property.Argument.Value;
                                        }
                                    }
                                }

                                ProcessMethodImport(method, il, symbol, callConv);
                            }

                            assembly.Write();
                            modified = true;
                        }
                    }
                }
            }
            finally
            {
                if (modified)
                {
                    EditorUtility.RequestScriptReload();
                }

                s_AssemblyPaths.Clear();
            }
        }

        private static void ProcessMethodImport(MethodDefinition method, ILProcessor il, IDiaSymbol symbol, CallingConvention callingConvention)
        {
            var site = new CallSite(method.ReturnType);
            method.Body.Variables.Add(new VariableDefinition(method.Module.TypeSystem.UIntPtr));
            il.Emit(OpCodes.Call, method.Module.ImportReference(typeof(UnityModuleHelpers).GetMethod("GetBaseAddress")));
            il.Emit(OpCodes.Ldc_I4, (int)symbol.relativeVirtualAddress);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_0);

            switch (callingConvention)
            {
            case CallingConvention.Cdecl:
                site.CallingConvention = MethodCallingConvention.C;
                break;
            case CallingConvention.StdCall:
            case CallingConvention.Winapi:
                site.CallingConvention = MethodCallingConvention.StdCall;
                break;
            case CallingConvention.FastCall:
                site.CallingConvention = MethodCallingConvention.FastCall;
                break;
            case CallingConvention.ThisCall:
                site.CallingConvention = MethodCallingConvention.ThisCall;
                break;
            }

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                site.Parameters.Add(method.Parameters[i]);
                il.Emit(OpCodes.Ldarg, i);
            }

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Calli, site);
            il.Emit(OpCodes.Ret);
        }

        private static void OnCompilationFinished(object context)
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                using (var process = Process.GetCurrentProcess())
                using (var module  = UnityModuleHelpers.GetUnityModule(process))
                {
                    ProcessAssemblies(module.FileName);
                }
            }
            else
            {
                if (BuildVariantExecutable != null)
                {
                    ProcessAssemblies(BuildVariantExecutable);
                }
            }
        }
    }
}
#endif
