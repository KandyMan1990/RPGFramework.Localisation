using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class LocalisationWriter
    {
        private const string MANIFEST_FILENAME = "manifest.locman";

        internal static async Task WriteAsync(LocalisationMaster master)
        {
            if (master == null)
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(WriteAsync)} Master is null");
            }

            if (master.SheetAssets == null || master.SheetAssets.Length == 0)
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(WriteAsync)} Master has no sheets");
            }

            if (!Enum.IsDefined(typeof(LocalisationVersion), master.Version))
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(WriteAsync)} Master [{master.name}] has an unset or unknown format version [{(int)master.Version}]. Choose one of: {string.Join(", ", Enum.GetNames(typeof(LocalisationVersion)))}");
            }

            try
            {
                UpdateProgress("Fetching localisation content...", 0f);

                List<LocalisationSheetContent> sheets = await FetchAllSheetsAsync(master);

                UpdateProgress("Validating localisation content...", 0.5f);

                ValidateSheets(master, sheets);

                UpdateProgress("Writing localisation bin file(s)...", 0.6f);

                WriteLocalisationBin(master, sheets);

                UpdateProgress("Writing localisation manifest file...", 0.75f);

                WriteLocalisationManifest(master, sheets);

                UpdateProgress("Generating localisation keys class file(s)...", 0.85f);

                WriteKeysToClassFiles(master, sheets);

                Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteAsync)} {master.name} file and class generation complete");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
        }

        internal static byte[] BuildV1SheetPayload(LocalisationSheetBinary bin)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            bw.Write((uint)bin.Hashes.Length);

            for (int i = 0; i < bin.Hashes.Length; i++)
            {
                bw.Write(bin.Hashes[i]);
                bw.Write(bin.Offsets[i]);
            }

            bw.Write(bin.StringTable);
            bw.Flush();

            return ms.ToArray();
        }

        private static async Task<List<LocalisationSheetContent>> FetchAllSheetsAsync(LocalisationMaster master)
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent>(master.SheetAssets.Length);

            for (int i = 0; i < master.SheetAssets.Length; i++)
            {
                LocalisationSheetAsset sheetAsset = master.SheetAssets[i];

                if (UpdateProgress($"Fetching sheet [{sheetAsset.SheetName}] ({i + 1} of {master.SheetAssets.Length})...",
                                   0.5f * i / master.SheetAssets.Length))
                {
                    throw new OperationCanceledException($"{nameof(LocalisationWriter)}::{nameof(FetchAllSheetsAsync)} Cancelled while fetching sheet [{sheetAsset.SheetName}]. Nothing was written");
                }

                string csv = await GoogleSheetDataProvider.GetCsv(master, sheetAsset);

                List<string[]> rows      = CsvParser.ParseCsv(csv);
                List<string>   languages = CsvParser.GetLanguages(rows[0]);

                CsvParser.GetValues(languages, rows, out List<string> keys, out List<string> _, out Dictionary<string, List<string>> values);

                sheets.Add(new LocalisationSheetContent(sheetAsset.SheetName, languages, keys, values));
            }

            return sheets;
        }

        private static void ValidateSheets(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            HashSet<string> seenSheetNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < sheets.Count; i++)
            {
                LocalisationSheetContent sheet     = sheets[i];
                LocalisationSheetAsset   asset     = master.SheetAssets[i];
                string                   sheetName = sheet.SheetName;

                if (!IsValidIdentifier(sheetName))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateSheets)} Sheet name [{sheetName}] on asset [{asset.name}] is not a valid C# identifier, and is emitted as a class name");
                }

                if (!seenSheetNames.Add(sheetName))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateSheets)} Two sheets are both named [{sheetName}]. Names must be unique: they scope every key and name the generated file");
                }

                string namespaceToUse = string.IsNullOrEmpty(asset.NamespaceOverride) ? master.DefaultNamespace : asset.NamespaceOverride;

                if (!IsValidNamespace(namespaceToUse))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateSheets)} Namespace [{namespaceToUse}] used by sheet [{sheetName}] is not a valid C# namespace");
                }

                ValidateGeneratedIdentifiers(sheetName, sheet.Keys);

                ReportEmptyValues(sheetName, sheet);
            }

            ValidateLanguageConsistency(master, sheets);
        }

        private static void ValidateLanguageConsistency(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            if (master.Version != LocalisationVersion.FilePerLanguage || sheets.Count < 2)
            {
                return;
            }

            List<string> expected = sheets[0].Languages;

            for (int i = 1; i < sheets.Count; i++)
            {
                List<string> actual = sheets[i].Languages;

                bool matches = actual.Count == expected.Count;

                for (int j = 0; matches && j < expected.Count; j++)
                {
                    matches = string.Equals(expected[j], actual[j], StringComparison.Ordinal);
                }

                if (!matches)
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateLanguageConsistency)} Sheet [{sheets[i].SheetName}] declares languages [{string.Join(", ", actual)}] but sheet [{sheets[0].SheetName}] declares [{string.Join(", ", expected)}]. The {nameof(LocalisationVersion.FilePerLanguage)} format needs the same languages, in the same order, in every sheet");
                }
            }
        }

        private static void ValidateGeneratedIdentifiers(string sheetName, List<string> keys)
        {
            Dictionary<string, string> seen = new Dictionary<string, string>(keys.Count, StringComparer.Ordinal);

            seen.Add("SHEET_NAME", "<generated sheet name constant>");

            foreach (string key in keys)
            {
                string identifier = SanitiseIdentifier(key);

                if (seen.TryGetValue(identifier, out string existing))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateGeneratedIdentifiers)} Sheet [{sheetName}]: keys [{key}] and [{existing}] both generate the identifier [{identifier}]. Rename one of them");
                }

                seen.Add(identifier, key);
            }
        }

        private static void ReportEmptyValues(string sheetName, LocalisationSheetContent sheet)
        {
            foreach (string language in sheet.Languages)
            {
                List<string> values = sheet.Values[language];
                int          empty  = 0;

                for (int i = 0; i < values.Count; i++)
                {
                    if (string.IsNullOrEmpty(values[i]))
                    {
                        empty++;
                    }
                }

                if (empty > 0)
                {
                    Debug.LogWarning($"{nameof(LocalisationWriter)}::{nameof(ReportEmptyValues)} Sheet [{sheetName}] language [{language}] has [{empty}] of [{values.Count}] entries untranslated. These render as empty text, not as a missing-key marker");
                }
            }
        }

        private static void WriteLocalisationBin(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            if (Directory.Exists(Constants.BasePath))
            {
                Directory.Delete(Constants.BasePath, true);
            }

            Directory.CreateDirectory(Constants.BasePath);

            ILocalisationBinWriter localisationBinWriter = LocalisationBinWriterProvider.GetLocalisationBinWriter((byte)master.Version);

            localisationBinWriter.GenerateLocalisationBin(sheets);
        }

        private static void WriteLocalisationManifest(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            List<string>    languages = new List<string>();
            HashSet<string> seen      = new HashSet<string>(StringComparer.Ordinal);

            foreach (LocalisationSheetContent sheet in sheets)
            {
                foreach (string language in sheet.Languages)
                {
                    if (seen.Add(language))
                    {
                        languages.Add(language);
                    }
                }
            }

            string manifestPath = Path.Combine(Constants.BasePath, MANIFEST_FILENAME);

            using (FileStream fs = new FileStream(manifestPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(Constants.LocManMagic);
                bw.Write((byte)master.Version);

                foreach (string language in languages)
                {
                    bw.Write(Encoding.UTF8.GetBytes(language));
                    bw.Write((byte)0);
                }

                bw.Flush();
            }

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteLocalisationManifest)} Wrote {manifestPath}");
        }

        private static void WriteKeysToClassFiles(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            for (int i = 0; i < sheets.Count; i++)
            {
                WriteGeneratedKeysClass(master.DefaultNamespace, master.SheetAssets[i], sheets[i].Keys);
            }
        }

        private static void WriteGeneratedKeysClass(string defaultNamespace, LocalisationSheetAsset asset, List<string> keys)
        {
            string outFolder = asset.GeneratedOutputFolder;

            if (string.IsNullOrEmpty(outFolder))
            {
                outFolder = Path.Combine(Application.dataPath, "GeneralisedLocalisation");
            }

            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }

            string namespaceToUse = string.IsNullOrEmpty(asset.NamespaceOverride) ? defaultNamespace : asset.NamespaceOverride;
            string sheetName      = asset.SheetName;

            string             path = Path.Combine(outFolder, $"{sheetName}.cs");
            using StreamWriter sw   = new StreamWriter(path, false, Encoding.UTF8);

            sw.WriteLine($"namespace {namespaceToUse}");
            sw.WriteLine("{");
            sw.WriteLine($"\t// Auto generated keys for sheet: {sheetName}");
            sw.WriteLine("\tpublic static partial class LocalisationKeys");
            sw.WriteLine("\t{");
            sw.WriteLine($"\t\tpublic static class {sheetName}");
            sw.WriteLine("\t\t{");

            sw.WriteLine($"\t\t\t///<summary>{EscapeXmlDoc(sheetName)}</summary>");
            sw.WriteLine($"\t\t\tpublic const string SHEET_NAME = @\"{EscapeVerbatim(sheetName)}\";");

            foreach (string key in keys)
            {
                string id           = SanitiseIdentifier(key);
                string qualifiedKey = $"{sheetName}{LocalisationBinaryBuilder.KEY_SEPARATOR}{key}";

                sw.WriteLine($"\t\t\t///<summary>{EscapeXmlDoc(key)}</summary>");
                sw.WriteLine($"\t\t\tpublic const string {id} = @\"{EscapeVerbatim(qualifiedKey)}\";");
            }

            sw.WriteLine("\t\t}");
            sw.WriteLine("\t}");
            sw.WriteLine("}");

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteGeneratedKeysClass)} Generated keys class at {path}");
        }

        private static string EscapeVerbatim(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static string EscapeXmlDoc(string value)
        {
            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;");
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || !(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidNamespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (string part in value.Split('.'))
            {
                if (!IsValidIdentifier(part))
                {
                    return false;
                }
            }

            return true;
        }

        private static string SanitiseIdentifier(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "KEY_EMPTY";
            }

            string[] parts = key.Split(new[]
                                       {
                                           '.',
                                           '/',
                                           ' ',
                                           '-'
                                       },
                                       StringSplitOptions.RemoveEmptyEntries);

            StringBuilder outName = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                string p = KeepIdentifierChars(parts[i]);

                if (p.Length == 0)
                {
                    continue;
                }

                if (outName.Length > 0)
                {
                    outName.Append('_');
                }

                outName.Append(p.ToUpperInvariant());
            }

            if (outName.Length == 0)
            {
                return "KEY";
            }

            if (!char.IsLetter(outName[0]))
            {
                outName.Insert(0, '_');
            }

            string identifier = outName.ToString();

            return identifier;
        }

        private static string KeepIdentifierChars(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) && c < 128 || c == '_')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static bool UpdateProgress(string message, float progress)
        {
            bool cancelled = EditorUtility.DisplayCancelableProgressBar("Localisation Writer", message, progress);

            return cancelled;
        }
    }
}