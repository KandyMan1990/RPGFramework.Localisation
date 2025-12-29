namespace RPGFramework.Localisation.Data
{
    internal sealed class LocalisationData
    {
        internal ulong[] Hashes      { get; }
        internal int[]   Offsets     { get; }
        internal byte[]  StringTable { get; }

        internal LocalisationData(ulong[] hashes, int[] offsets, byte[] stringTable)
        {
            Hashes      = hashes;
            Offsets     = offsets;
            StringTable = stringTable;
        }
    }
}