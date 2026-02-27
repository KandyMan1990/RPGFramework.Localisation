using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    /// <summary>
    /// Generates per-sheet per-language .locbin files and a generated C# key class from a Google Sheet (CSV export).<br></br>
    /// Layout:<br></br>
    /// Column A: key<br></br>
    /// Column B: comments<br></br>
    /// Column C+: languages (headers in row 1)
    /// </summary>
    internal sealed class LocalisationBinWriter_Version02 : ILocalisationBinWriter
    {
        private const byte VERSION = 2;

        void ILocalisationBinWriter.GenerateLocalisationBin(List<LocalisationSheetContent> dataToWrite)
        {
            List<string> languages = dataToWrite[0].Languages;

            for (int i = 1; i < dataToWrite.Count; i++)
            {
                if (!languages.SequenceEqual(dataToWrite[i].Languages))
                {
                    throw new ArgumentException($"{nameof(LocalisationBinWriter_Version02)}::{nameof(ILocalisationBinWriter.GenerateLocalisationBin)} All sheets should include the same languages in the same order");
                }
            }

            foreach (string language in languages)
            {
                string filePath = Path.Combine(Constants.BasePath, $"{language}.locbin");

                List<(string sheetName, LocalisationSheetBinary bin)> bins = new List<(string sheetName, LocalisationSheetBinary bin)>();

                foreach (LocalisationSheetContent data in dataToWrite)
                {
                    List<string>            values = data.Values[language];
                    LocalisationSheetBinary bin    = LocalisationBinaryBuilder.BuildBinary(data.Keys, values);
                    bins.Add((data.SheetName, bin));
                }

                WriteLocBin(language, filePath, bins);

                Debug.Log($"{nameof(LocalisationBinWriter_Version02)}::{nameof(ILocalisationBinWriter.GenerateLocalisationBin)} Wrote {filePath}");
            }
        }

        private static void WriteLocBin(string language, string filePath, List<(string sheetName, LocalisationSheetBinary bin)> bins)
        {
            int sheetCount = bins.Count;

            byte[][] payloads = new byte[sheetCount][];
            uint[]   lengths  = new uint[sheetCount];
            ulong[]  hashes   = new ulong[sheetCount];

            for (int i = 0; i < sheetCount; i++)
            {
                payloads[i] = LocalisationWriter.BuildV1SheetPayload(bins[i].bin);
                lengths[i]  = (uint)payloads[i].Length;
                hashes[i]   = Fnv1a64.Hash(bins[i].sheetName);
            }

            using FileStream   fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(Constants.LocBinMagic);
            bw.Write(VERSION);
            bw.Write((byte)Encoding.UTF8.GetByteCount(language));
            bw.Write(Encoding.UTF8.GetBytes(language));

            bw.Write((uint)sheetCount);

            long tocStart = fs.Position;
            long tocSize  = sheetCount * (sizeof(ulong) + sizeof(uint) + sizeof(uint));

            long payloadStart = tocStart + tocSize;

            uint runningOffset = (uint)payloadStart;

            for (int i = 0; i < sheetCount; i++)
            {
                bw.Write(hashes[i]);
                bw.Write(runningOffset);
                bw.Write(lengths[i]);

                runningOffset += lengths[i];
            }

            for (int i = 0; i < sheetCount; i++)
            {
                bw.Write(payloads[i]);
            }

            bw.Flush();
        }
    }
}