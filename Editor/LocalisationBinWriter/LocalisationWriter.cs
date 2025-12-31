using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            UpdateProgress("Writing localisation bin file(s)...", 0f);

            await WriteLocalisationBinAsync(master);

            UpdateProgress("Writing localisation manifest file...", 0.33f);

            await WriteLocalisationManifestAsync(master);

            UpdateProgress("Generating localisation keys class file(s)...", 0.67f);

            await WriteKeysToClassFile(master);

            UpdateProgress("Refreshing assets...", 0.9f);

            AssetDatabase.Refresh();

            UpdateProgress("Done", 1f);

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteAsync)} {master.name} file and class generation complete");
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

        private static async Task WriteLocalisationBinAsync(LocalisationMaster master)
        {
            List<LocalisationSheetContent> dataToWrite = new List<LocalisationSheetContent>(master.SheetAssets.Length);

            foreach (LocalisationSheetAsset sheetAsset in master.SheetAssets)
            {
                string csv = await GoogleSheetDataProvider.GetCsv(master, sheetAsset);

                List<string[]> rows = CsvParser.ParseCsv(csv);

                List<string> languages = CsvParser.GetLanguages(rows[0]);

                CsvParser.GetValues(languages, rows, out List<string> keys, out List<string> comments, out Dictionary<string, List<string>> values);

                dataToWrite.Add(new LocalisationSheetContent(sheetAsset.SheetName, languages, keys, values));
            }

            if (Directory.Exists(Constants.BasePath))
            {
                Directory.Delete(Constants.BasePath, true);
            }

            Directory.CreateDirectory(Constants.BasePath);

            ILocalisationBinWriter localisationBinWriter = LocalisationBinWriterProvider.GetLocalisationBinWriter((byte)master.Version);

            localisationBinWriter.GenerateLocalisationBin(dataToWrite);
        }

        private static async Task WriteLocalisationManifestAsync(LocalisationMaster master)
        {
            Dictionary<string, string> languages = new Dictionary<string, string>();

            foreach (LocalisationSheetAsset sheetAsset in master.SheetAssets)
            {
                string csv = await GoogleSheetDataProvider.GetCsv(master, sheetAsset);

                List<string[]> rows = CsvParser.ParseCsv(csv);

                List<string> sheetLanguages = CsvParser.GetLanguages(rows[0]);

                foreach (string language in sheetLanguages)
                {
                    languages.TryAdd(language, language);
                }
            }

            string manifestPath = Path.Combine(Constants.BasePath, MANIFEST_FILENAME);

            WriteLocalisationManifest(manifestPath, languages.Select(l => l.Key).ToList(), (byte)master.Version);

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteLocalisationManifestAsync)} Wrote {manifestPath}");
        }

        private static void WriteLocalisationManifest(string path, IReadOnlyList<string> languages, byte version)
        {
            using FileStream   fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(Constants.LocManMagic);
            bw.Write(version);

            foreach (string language in languages)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(language);
                bw.Write(bytes);
                bw.Write((byte)0);
            }

            bw.Flush();
        }

        private static async Task WriteKeysToClassFile(LocalisationMaster master)
        {
            foreach (LocalisationSheetAsset sheetAsset in master.SheetAssets)
            {
                string csv = await GoogleSheetDataProvider.GetCsv(master, sheetAsset);

                List<string[]> rows = CsvParser.ParseCsv(csv);

                List<string> languages = CsvParser.GetLanguages(rows[0]);

                CsvParser.GetValues(languages, rows, out List<string> keys, out List<string> comments, out Dictionary<string, List<string>> values);

                WriteGeneratedKeysClass(master.DefaultNamespace, sheetAsset, keys);
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

            string             path = Path.Combine(outFolder, $"{asset.SheetName}.cs");
            using StreamWriter sw   = new StreamWriter(path, false, Encoding.UTF8);

            sw.WriteLine($"namespace {namespaceToUse}");
            sw.WriteLine("{");
            sw.WriteLine($"\t// Auto generated keys for sheet: {asset.SheetName}");
            sw.WriteLine("\tpublic static partial class LocalisationKeys");
            sw.WriteLine("\t{");
            sw.WriteLine($"\t\tpublic static class {asset.SheetName}");
            sw.WriteLine("\t\t{");

            sw.WriteLine($"\t\t\t///<summary>{asset.SheetName}</summary>");
            sw.WriteLine($"\t\t\tpublic const string SHEET_NAME = @\"{asset.SheetName}\";");

            foreach (string key in keys)
            {
                string id = SanitiseIdentifier(key);
                sw.WriteLine($"\t\t\t///<summary>{key}</summary>");
                sw.WriteLine($"\t\t\tpublic const string {id} = @\"{asset.SheetName}/{key}\";");
            }

            sw.WriteLine("\t\t}");
            sw.WriteLine("\t}");
            sw.WriteLine("}");

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteGeneratedKeysClass)} Generated keys class at {path}");
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

            string outName = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                p = Regex.Replace(p, "[^A-Za-z0-9_]", "");

                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }

                if (i == 0)
                {
                    outName += p.ToUpperInvariant();
                }
                else
                {
                    outName += "_" + p.ToUpperInvariant();
                }
            }

            if (string.IsNullOrEmpty(outName))
            {
                outName = "KEY";
            }

            if (!char.IsLetter(outName[0]))
            {
                outName = "_" + outName;
            }

            return outName;
        }

        private static void UpdateProgress(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("Localisation Writer", message, progress);
        }
    }
}