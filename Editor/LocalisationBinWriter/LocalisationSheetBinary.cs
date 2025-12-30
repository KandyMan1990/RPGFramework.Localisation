namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal readonly struct LocalisationSheetBinary
    {
        internal readonly ulong[] Hashes;
        internal readonly int[]   Offsets;
        internal readonly byte[]  StringTable;

        internal LocalisationSheetBinary(ulong[] hashes, int[] offsets, byte[] stringTable)
        {
            Hashes      = hashes;
            Offsets     = offsets;
            StringTable = stringTable;
        }
    }
}