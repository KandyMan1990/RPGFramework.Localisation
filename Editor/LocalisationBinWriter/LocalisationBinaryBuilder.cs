using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RPGFramework.Hashing;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class LocalisationBinaryBuilder
    {
        internal const char KEY_SEPARATOR = '/';

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

        internal static LocalisationSheetBinary BuildBinary(string sheetName, List<string> keys, List<string> values)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet name is null or empty");
            }

            if (sheetName.IndexOf(KEY_SEPARATOR) >= 0)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet name [{sheetName}] contains '{KEY_SEPARATOR}', which is reserved as the sheet/key separator");
            }

            if (keys.Count != values.Count)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet [{sheetName}] has [{keys.Count}] keys but [{values.Count}] values");
            }

            int count = keys.Count;

            using MemoryStream stringTable = new MemoryStream();
            using BinaryWriter writer      = new BinaryWriter(stringTable, Encoding.UTF8);

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

            Dictionary<ulong, string> seenHashes = new Dictionary<ulong, string>(count);
            HashSet<string>           seenKeys   = new HashSet<string>(count, StringComparer.Ordinal);

            for (int i = 0; i < count; i++)
            {
                string key = keys[i];

                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet [{sheetName}] row [{i + 1}] has an empty key");
                }

                if (key.IndexOf(KEY_SEPARATOR) >= 0)
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet [{sheetName}] key [{key}] contains '{KEY_SEPARATOR}', which is reserved as the sheet/key separator");
                }

                if (!seenKeys.Add(key))
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Sheet [{sheetName}] declares the key [{key}] more than once");
                }

                string qualifiedKey = $"{sheetName}{KEY_SEPARATOR}{key}";
                ulong  hash         = Fnv1a64.Hash(qualifiedKey);

                if (seenHashes.TryGetValue(hash, out string existing))
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinaryBuilder)}::{nameof(BuildBinary)} Hash collision in sheet [{sheetName}]: [{key}] and [{existing}] both hash to [{hash}]. Rename one of them");
                }

                seenHashes.Add(hash, key);

                pairs[i] = new HashOffsetPair(hash, offsets[i]);
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