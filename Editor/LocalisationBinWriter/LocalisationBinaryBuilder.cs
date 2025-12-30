using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RPGFramework.Localisation.Editor.Helpers;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class LocalisationBinaryBuilder
    {
        private readonly struct HashOffsetPair
        {
            internal readonly ulong Hash;
            internal readonly int   Offset;

            internal HashOffsetPair(ulong hash, int offset)
            {
                Hash   = hash;
                Offset = offset;
            }
        }

        internal static LocalisationSheetBinary BuildBinary(List<string> keys, List<string> values)
        {
            using MemoryStream stringTable = new MemoryStream();
            using BinaryWriter writer      = new BinaryWriter(stringTable, Encoding.UTF8);

            int count = keys.Count;

            int[] offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(values[i]);
                offsets[i] = (int)stringTable.Position;

                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            writer.Flush();

            byte[] tableBytes = stringTable.ToArray();

            HashOffsetPair[] pairs = new HashOffsetPair[count];
            for (int i = 0; i < count; i++)
            {
                pairs[i] = new HashOffsetPair(Fnv1a64.Hash(keys[i]), offsets[i]);
            }

            Array.Sort(pairs, (a, b) => a.Hash.CompareTo(b.Hash));

            ulong[] hashes        = new ulong[count];
            int[]   sortedOffsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]        = pairs[i].Hash;
                sortedOffsets[i] = pairs[i].Offset;
            }

            return new LocalisationSheetBinary(hashes, sortedOffsets, tableBytes);
        }
    }
}