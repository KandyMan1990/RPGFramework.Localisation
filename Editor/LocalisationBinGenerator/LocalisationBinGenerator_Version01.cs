using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    /// <summary>
    /// One .locbin per sheet per language.<br></br>
    /// Layout:<br></br>
    /// Column A: key<br></br>
    /// Column B: comments<br></br>
    /// Column C+: languages (headers in row 1)
    /// </summary>
    internal sealed class LocalisationBinGenerator_Version01 : ILocalisationBinGenerator
    {
        private const byte VERSION = 1;

        List<LocalisationBinFile> ILocalisationBinGenerator.BuildLocalisationBin(List<LocalisationSheetContent> dataToWrite)
        {
            List<LocalisationBinFile> files = new List<LocalisationBinFile>();

            foreach (LocalisationSheetContent data in dataToWrite)
            {
                foreach (string language in data.Languages)
                {
                    List<string>            values   = data.Values[language];
                    LocalisationSheetBinary bin      = LocalisationBinaryBuilder.BuildBinary(data.SheetName, data.Keys, values);
                    string                  filePath = Path.Combine(Constants.BasePath, language, $"{data.SheetName}.locbin");

                    files.Add(new LocalisationBinFile(filePath, BuildLocBin(language, bin)));
                }
            }

            return files;
        }

        private static byte[] BuildLocBin(string language, LocalisationSheetBinary bin)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            byte[] payload = LocalisationWriter.BuildV1SheetPayload(bin);

            bw.Write(Constants.LocBinMagic);
            bw.Write(VERSION);
            bw.Write((byte)Encoding.UTF8.GetByteCount(language));
            bw.Write(Encoding.UTF8.GetBytes(language));
            bw.Write(payload);
            bw.Flush();

            byte[] bytes = ms.ToArray();

            return bytes;
        }
    }
}