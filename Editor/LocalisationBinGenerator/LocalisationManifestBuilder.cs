using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    /// <summary>
    /// The manifest the runtime reads first: the files' format version, and every language the sheets declare, in the
    /// order they first appear.
    /// </summary>
    internal static class LocalisationManifestBuilder
    {
        internal static byte[] Build(byte version, List<LocalisationSheetContent> sheets)
        {
            List<string>    languages = new List<string>();
            HashSet<string> seen      = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < sheets.Count; i++)
            {
                LocalisationSheetContent sheet = sheets[i];

                for (int j = 0; j < sheet.Languages.Count; j++)
                {
                    string language = sheet.Languages[j];

                    if (seen.Add(language))
                    {
                        languages.Add(language);
                    }
                }
            }

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(Constants.LocManMagic);
            writer.Write(version);

            for (int i = 0; i < languages.Count; i++)
            {
                writer.Write(Encoding.UTF8.GetBytes(languages[i]));
                writer.Write((byte)0);
            }

            writer.Flush();

            byte[] bytes = stream.ToArray();

            return bytes;
        }
    }
}
