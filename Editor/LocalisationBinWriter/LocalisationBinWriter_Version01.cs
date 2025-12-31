using System.Collections.Generic;
using System.IO;
using System.Text;
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
    internal sealed class LocalisationBinWriter_Version01 : ILocalisationBinWriter
    {
        private const byte VERSION = 1;

        void ILocalisationBinWriter.GenerateLocalisationBin(List<LocalisationSheetContent> dataToWrite)
        {
            foreach (LocalisationSheetContent data in dataToWrite)
            {
                foreach (string language in data.Languages)
                {
                    string folderPath = Path.Combine(Constants.BasePath, language);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    List<string>            values   = data.Values[language];
                    string                  filePath = Path.Combine(folderPath, $"{data.SheetName}.locbin");
                    LocalisationSheetBinary bin      = LocalisationBinaryBuilder.BuildBinary(data.Keys, values);

                    WriteLocBin(language, filePath, bin);

                    Debug.Log($"{nameof(LocalisationBinWriter_Version01)}::{nameof(ILocalisationBinWriter.GenerateLocalisationBin)} Wrote {filePath}");
                }
            }
        }

        private static void WriteLocBin(string language, string filePath, LocalisationSheetBinary bin)
        {
            using FileStream   fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs);

            byte[] payload = LocalisationWriter.BuildV1SheetPayload(bin);

            bw.Write(Constants.LocBinMagic);
            bw.Write(VERSION);
            bw.Write((byte)Encoding.UTF8.GetByteCount(language));
            bw.Write(Encoding.UTF8.GetBytes(language));
            bw.Write(payload);
            bw.Flush();
        }
    }
}