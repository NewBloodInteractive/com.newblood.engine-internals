#if UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Dia2Lib;
using BindingFlags = System.Reflection.BindingFlags;

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

            try
            {
                IDiaSession session;
                var source = Dia2.CreateDataSource();
                source.loadDataForExe(executable, null, null);
                source.openSession(out session);
                var globalScope = session.globalScope;

                var methods    = new List<MethodDefinition>();
                var attributes = new Dictionary<MemberReference, CustomAttribute>();

                foreach (string assemblyPath in s_AssemblyPaths)
                {
                    using (var resolver = new DefaultAssemblyResolver())
                    {
                        foreach (string path in CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All))
                            resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(path));

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
                                foreach (PropertyDefinition property in type.Properties)
                                {
                                    foreach (CustomAttribute attribute in property.GetMethod.CustomAttributes)
                                    {
                                        if (attribute.AttributeType.FullName != typeof(EngineImportAttribute).FullName)
                                            continue;

                                        if (property.SetMethod != null)
                                        {
                                            Debug.LogErrorFormat("[EngineImport] Property must be get-only: {0}", property.FullName);
                                            continue;
                                        }

                                        if (property.GetMethod == null)
                                        {
                                            Debug.LogErrorFormat("[EngineImport] Property must have getter: {0}", property.FullName);
                                            continue;
                                        }

                                        if (!property.GetMethod.IsStatic)
                                        {
                                            Debug.LogErrorFormat("[EngineImport] Property must be static: {0}", property.FullName);
                                            continue;
                                        }

                                        if (!property.PropertyType.IsPointer &&
                                            !property.PropertyType.IsFunctionPointer &&
                                            property.PropertyType != assembly.MainModule.TypeSystem.IntPtr &&
                                            property.PropertyType != assembly.MainModule.TypeSystem.UIntPtr)
                                        {
                                            Debug.LogErrorFormat("[EngineImport] Property must return pointer or native integer type: {0}", property.FullName);
                                            continue;
                                        }

                                        if (property.GetMethod.RVA != 0)
                                            Debug.LogWarningFormat("[EngineImport] Property is not marked as extern: {0}", property.FullName);

                                        methods.Add(property.GetMethod);
                                        attributes.Add(property, attribute);
                                        break;
                                    }
                                }

                                foreach (MethodDefinition method in type.Methods)
                                {
                                    foreach (CustomAttribute attribute in method.CustomAttributes)
                                    {
                                        if (attribute.AttributeType.FullName != typeof(EngineImportAttribute).FullName)
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

                            var imports = new TypeDefinition("", "<UnityEngineImports>",
                                TypeAttributes.Abstract |
                                TypeAttributes.Sealed |
                                TypeAttributes.BeforeFieldInit,
                                assembly.MainModule.TypeSystem.Object);

                            var cctor = new MethodDefinition(".cctor",
                                MethodAttributes.Private |
                                MethodAttributes.Static |
                                MethodAttributes.HideBySig |
                                MethodAttributes.RTSpecialName |
                                MethodAttributes.SpecialName,
                                assembly.MainModule.TypeSystem.Void);

                            imports.Methods.Add(cctor);
                            var importIL    = cctor.Body.GetILProcessor();
                            var baseAddress = assembly.MainModule.ImportReference(typeof(EngineImportHelpers).GetProperty(nameof(EngineImportHelpers.BaseAddress)).GetMethod);

                            for (int i = 0, j = 0; i < methods.Count; i++)
                            {
                                CustomAttributeArgument argument;
                                var method    = methods[i];
                                var attribute = attributes[method];
                                var name      = attribute.ConstructorArguments[0].Value.ToString();
                                method.CustomAttributes.Remove(attribute);

                                IDiaEnumSymbols enumerator;
                                globalScope.findChildren(SymTagEnum.SymTagPublicSymbol, name, 0, out enumerator);
                                
                                method.Body = new MethodBody(method);
                                var il      = method.Body.GetILProcessor();

                                if (enumerator.count <= 0)
                                {
                                    Debug.LogWarningFormat("Could not resolve symbol: {0}", name);
                                    il.Emit(OpCodes.Ldstr, $"Unable to find an entry point named '{name}'.");
                                    il.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(typeof(EntryPointNotFoundException).GetConstructor(new[] { typeof(string) })));
                                    il.Emit(OpCodes.Throw);
                                    continue;
                                }

                                var symbol  = enumerator.Item(0);
                                var address = symbol.relativeVirtualAddress;

                                if (method.IsGetter)
                                {
                                    il.Emit(OpCodes.Call, baseAddress);
                                    il.Emit(OpCodes.Ldc_I4, (int)address);
                                    il.Emit(OpCodes.Add);
                                    il.Emit(OpCodes.Ret);
                                    continue;
                                }

                                if (attribute.HasProperties)
                                    argument = attribute.Properties[0].Argument;
                                else
                                    argument = new CustomAttributeArgument(assembly.MainModule.ImportReference(typeof(CallingConvention)), CallingConvention.Winapi);

                                var type = new TypeDefinition("", $"Delegate{j}",
                                    TypeAttributes.Sealed |
                                    TypeAttributes.Public,
                                    assembly.MainModule.ImportReference(typeof(MulticastDelegate)));

                                var call = new CustomAttribute(assembly.MainModule.ImportReference(typeof(UnmanagedFunctionPointerAttribute).GetConstructors()[0]))
                                {
                                    ConstructorArguments = { argument }
                                };

                                type.CustomAttributes.Add(call);

                                var ctor = new MethodDefinition(".ctor",
                                    MethodAttributes.FamANDAssem |
                                    MethodAttributes.Family |
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.RTSpecialName |
                                    MethodAttributes.SpecialName,
                                    assembly.MainModule.TypeSystem.Void)
                                {
                                    IsRuntime  = true,
                                    Parameters =
                                    {
                                        new ParameterDefinition(assembly.MainModule.TypeSystem.Object),
                                        new ParameterDefinition(assembly.MainModule.TypeSystem.IntPtr)
                                    }
                                };

                                var invoke = new MethodDefinition("Invoke",
                                    MethodAttributes.Public |
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.NewSlot |
                                    MethodAttributes.Virtual,
                                    method.ReturnType)
                                {
                                    HasThis          = true,
                                    IsRuntime        = true,
                                    MethodReturnType = method.MethodReturnType
                                };
                                
                                foreach (var parameter in method.Parameters)
                                    invoke.Parameters.Add(parameter);

                                var beginInvoke = new MethodDefinition("BeginInvoke",
                                    MethodAttributes.Public |
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.NewSlot |
                                    MethodAttributes.Virtual,
                                    assembly.MainModule.ImportReference(typeof(IAsyncResult)))
                                {
                                    HasThis   = true,
                                    IsRuntime = true
                                };

                                foreach (var parameter in method.Parameters)
                                    beginInvoke.Parameters.Add(parameter);

                                beginInvoke.Parameters.Add(new ParameterDefinition(assembly.MainModule.ImportReference(typeof(AsyncCallback))));
                                beginInvoke.Parameters.Add(new ParameterDefinition(assembly.MainModule.TypeSystem.Object));

                                var endInvoke = new MethodDefinition("EndInvoke",
                                    MethodAttributes.Public |
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.NewSlot |
                                    MethodAttributes.Virtual,
                                    method.ReturnType)
                                {
                                    HasThis          = true,
                                    IsRuntime        = true,
                                    Parameters       = { new ParameterDefinition("ar", ParameterAttributes.None, assembly.MainModule.ImportReference(typeof(IAsyncResult))) },
                                    MethodReturnType = method.MethodReturnType
                                };

                                type.Methods.Add(ctor);
                                type.Methods.Add(invoke);
                                type.Methods.Add(beginInvoke);
                                type.Methods.Add(endInvoke);

                                var field = new FieldDefinition($"Instance{j}",
                                    FieldAttributes.Public |
                                    FieldAttributes.Static |
                                    FieldAttributes.InitOnly,
                                    type);

                                importIL.Emit(OpCodes.Call, baseAddress);
                                importIL.Emit(OpCodes.Ldc_I4, (int)address);
                                importIL.Emit(OpCodes.Add);
                                importIL.Emit(OpCodes.Ldtoken, type);
                                importIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")));
                                importIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(typeof(EngineImportHelpers).GetMethod("GetDelegate")));
                                importIL.Emit(OpCodes.Castclass, type);
                                importIL.Emit(OpCodes.Stsfld, field);

                                il.Emit(OpCodes.Ldsfld, field);

                                for (int p = 0; p < method.Parameters.Count; p++)
                                    il.Emit(OpCodes.Ldarg, p);

                                il.Emit(OpCodes.Callvirt, invoke);
                                il.Emit(OpCodes.Ret);

                                imports.Fields.Add(field);
                                imports.NestedTypes.Add(type);
                                j++;
                            }

                            importIL.Emit(OpCodes.Ret);
                            assembly.MainModule.Types.Add(imports);
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

        private static void OnCompilationFinished(object context)
        {
            if (!BuildPipeline.isBuildingPlayer)
                ProcessAssemblies(EngineImportHelpers.MainModuleFileName);
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
