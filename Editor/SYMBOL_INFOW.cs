namespace NewBlood
{
    unsafe struct SYMBOL_INFOW
    {
        public uint SizeOfStruct;
        public uint TypeIndex;
        public fixed ulong Reserved[2];
        public uint Index;
        public uint Size;
        public ulong ModBase;
        public uint Flags;
        public ulong Value;
        public ulong Address;
        public uint Register;
        public uint Scope;
        public uint Tag;
        public uint NameLen;
        public uint MaxNameLen;
        public fixed ushort Name[1];
    }
}
