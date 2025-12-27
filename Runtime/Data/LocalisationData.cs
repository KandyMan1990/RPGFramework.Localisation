using System;

namespace RPGFramework.Localisation.Data
{
    internal sealed class LocalisationData : IDisposable
    {
        public ulong[] Hashes      { get; private set; }
        public int[]   Offsets     { get; private set; }
        public byte[]  StringTable { get; private set; }

        internal LocalisationData(ulong[] hashes, int[] offsets, byte[] stringTable)
        {
            Hashes      = hashes;
            Offsets     = offsets;
            StringTable = stringTable;
        }

        public void Dispose()
        {
            Hashes      = null;
            Offsets     = null;
            StringTable = null;
        }
    }
}