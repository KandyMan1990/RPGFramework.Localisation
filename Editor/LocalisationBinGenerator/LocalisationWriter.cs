using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class LocalisationWriter
    {
        private const string SHEETS_FOLDER = "Sheets";
        private const string PULL_FOLDER   = "Library/RPGFramework.Localisation";

        // Written into every generated keys class, so only a class this writer made is ever deleted.
        private const string KEYS_CLASS_MARKER = "// Auto generated keys for sheet: ";

        /// <summary>
        /// Downloads every tab of the master's spreadsheet and brings the sheet assets in line with them: each tab is
        /// matched to the sheet asset named after it — updated in place, since other assets reference it — or given a new
        /// one in the Sheets folder beside the master, and its keys are recorded on it. The download is kept in Library,
        /// unversioned, for <see cref="Generate" /> to build from.
        /// </summary>
        internal static async Task PullAsync(LocalisationMaster master)
        {
            if (master == null)
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(PullAsync)} Master is null");
            }

            List<LocalisationSheetAsset> gone;

            try
            {
                UpdateProgress("Fetching the spreadsheet...", 0f);

                byte[] workbook = await GoogleSheetDataProvider.GetWorkbookAsync(master.SheetId);

                if (UpdateProgress("Reading the sheets...", 0.5f))
                {
                    throw new OperationCanceledException($"{nameof(LocalisationWriter)}::{nameof(PullAsync)} Cancelled after fetching the spreadsheet. Nothing was changed");
                }

                List<LocalisationSheetContent> sheets = SheetParser.ReadSheets(XlsxReader.Read(workbook));
                LocalisationSheetAsset[]       assets = FindSheetAssets(master, sheets, out gone);

                ValidateSheetNames(sheets);

                UpdateProgress("Updating the sheet assets...", 0.75f);

                PlaceSheetAssets(master, sheets, assets);
                RecordKeysOnSheets(master, sheets);
                SavePull(master, workbook);

                Debug.Log($"{nameof(LocalisationWriter)}::{nameof(PullAsync)} {master.name} pulled [{sheets.Count}] sheet(s)");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            OfferToDeleteGoneSheets(master, gone);
        }

        /// <summary>
        /// Builds the .locbin files, the manifest and the keys classes from the last pull, with no network. Nothing is
        /// written unless every sheet passes.
        /// </summary>
        internal static void Generate(LocalisationMaster master)
        {
            if (master == null)
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(Generate)} Master is null");
            }

            if (!Enum.IsDefined(typeof(LocalisationVersion), master.Version))
            {
                throw new ArgumentException($"{nameof(LocalisationWriter)}::{nameof(Generate)} Master [{master.name}] has an unset or unknown format version [{(int)master.Version}]. Choose one of: {string.Join(", ", Enum.GetNames(typeof(LocalisationVersion)))}");
            }

            string pulled = GetPullPath(master);

            if (!File.Exists(pulled))
            {
                throw new FileNotFoundException($"{nameof(LocalisationWriter)}::{nameof(Generate)} Nothing has been pulled for [{master.name}] on this machine. Pull first");
            }

            try
            {
                UpdateProgress("Reading the last pull...", 0f);

                List<LocalisationSheetContent> sheets = SheetParser.ReadSheets(XlsxReader.Read(File.ReadAllBytes(pulled)));
                LocalisationSheetAsset[]       assets = GetPulledSheetAssets(master, sheets);

                UpdateProgress("Validating localisation content...", 0.2f);

                ValidateSheetNames(sheets);
                ValidateSheets(master, sheets, assets);

                UpdateProgress("Writing localisation bin file(s)...", 0.4f);

                WriteLocalisationBin(master, sheets);

                UpdateProgress("Writing localisation manifest file...", 0.7f);

                WriteLocalisationManifest(master, sheets);

                UpdateProgress("Generating localisation keys class file(s)...", 0.85f);

                WriteKeysToClassFiles(master, sheets);

                Debug.Log($"{nameof(LocalisationWriter)}::{nameof(Generate)} {master.name} file and class generation complete");
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

        /// <returns>
        /// Each sheet's asset, in the sheets' order, or null where a sheet has none yet. The candidates are the master's
        /// list and the Sheets folder; <paramref name="gone" /> is those no tab matched.
        /// </returns>
        private static LocalisationSheetAsset[] FindSheetAssets(LocalisationMaster master, List<LocalisationSheetContent> sheets, out List<LocalisationSheetAsset> gone)
        {
            List<LocalisationSheetAsset> candidates = new List<LocalisationSheetAsset>();

            for (int i = 0; master.SheetAssets != null && i < master.SheetAssets.Length; i++)
            {
                if (master.SheetAssets[i] != null && !candidates.Contains(master.SheetAssets[i]))
                {
                    candidates.Add(master.SheetAssets[i]);
                }
            }

            string folder = GetSheetsFolder(master);

            if (AssetDatabase.IsValidFolder(folder))
            {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(LocalisationSheetAsset)}", new[] { folder });

                for (int i = 0; i < guids.Length; i++)
                {
                    LocalisationSheetAsset asset = AssetDatabase.LoadAssetAtPath<LocalisationSheetAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));

                    if (asset != null && !candidates.Contains(asset))
                    {
                        candidates.Add(asset);
                    }
                }
            }

            Dictionary<string, LocalisationSheetAsset> byName = new Dictionary<string, LocalisationSheetAsset>(StringComparer.Ordinal);

            for (int i = 0; i < candidates.Count; i++)
            {
                string sheetName = candidates[i].SheetName;

                if (byName.TryGetValue(sheetName, out LocalisationSheetAsset other))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(FindSheetAssets)} Sheet assets [{AssetDatabase.GetAssetPath(other)}] and [{AssetDatabase.GetAssetPath(candidates[i])}] are both for sheet [{sheetName}]. Delete one");
                }

                byName.Add(sheetName, candidates[i]);
            }

            LocalisationSheetAsset[] assets = new LocalisationSheetAsset[sheets.Count];

            for (int i = 0; i < sheets.Count; i++)
            {
                byName.TryGetValue(sheets[i].SheetName, out assets[i]);
            }

            gone = new List<LocalisationSheetAsset>();

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Array.IndexOf(assets, candidates[i]) < 0)
                {
                    gone.Add(candidates[i]);
                }
            }

            return assets;
        }

        /// <summary>
        /// Gives each new sheet an asset, moves every sheet's asset into the Sheets folder — a move keeps what references
        /// it — and rewrites the master's list to the sheets.
        /// </summary>
        private static void PlaceSheetAssets(LocalisationMaster master, List<LocalisationSheetContent> sheets, LocalisationSheetAsset[] assets)
        {
            string folder = GetSheetsFolder(master);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(GetAssetFolder(folder), SHEETS_FOLDER);
            }

            for (int i = 0; i < assets.Length; i++)
            {
                // The asset's name is the sheet's, so it must land at exactly this path, never a uniquified one.
                string target = $"{folder}/{sheets[i].SheetName}.asset";

                if (assets[i] == null)
                {
                    LocalisationSheetAsset asset = ScriptableObject.CreateInstance<LocalisationSheetAsset>();

                    asset.name = sheets[i].SheetName;

                    RefuseOccupied(target);
                    AssetDatabase.CreateAsset(asset, target);

                    assets[i] = asset;

                    continue;
                }

                string path = AssetDatabase.GetAssetPath(assets[i]);

                if (path == target)
                {
                    continue;
                }

                RefuseOccupied(target);

                string error = AssetDatabase.MoveAsset(path, target);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new IOException($"{nameof(LocalisationWriter)}::{nameof(PlaceSheetAssets)} Could not move [{path}] into [{folder}]: {error}");
                }
            }

            master.SheetAssets = assets;

            EditorUtility.SetDirty(master);
            AssetDatabase.SaveAssetIfDirty(master);
        }

        private static void RefuseOccupied(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new IOException($"{nameof(LocalisationWriter)}::{nameof(RefuseOccupied)} [{path}] is already taken by an asset that is not this sheet's. Move or rename it");
            }
        }

        private static void OfferToDeleteGoneSheets(LocalisationMaster master, List<LocalisationSheetAsset> gone)
        {
            if (gone.Count == 0)
            {
                return;
            }

            List<string>  paths = new List<string>();
            StringBuilder names = new StringBuilder();

            for (int i = 0; i < gone.Count; i++)
            {
                LocalisationSheetAsset asset     = gone[i];
                string                 keysClass = GetKeysClassPath(master, asset);

                paths.Add(AssetDatabase.GetAssetPath(asset));
                names.AppendLine(asset.name);

                if (keysClass != null)
                {
                    paths.Add(keysClass);
                }
            }

            bool delete = EditorUtility.DisplayDialog("Sheets no longer in the spreadsheet",
                                                      $"These sheet assets have no tab in the spreadsheet any more:\n\n{names}\nDelete them, and their generated keys classes? Anything that still uses one, such as a field's sheets, loses it.",
                                                      "Delete",
                                                      "Keep");

            if (!delete)
            {
                Debug.LogWarning($"{nameof(LocalisationWriter)}::{nameof(OfferToDeleteGoneSheets)} Kept sheet assets with no tab in the spreadsheet, which are left out of generation:\n{names}");

                return;
            }

            List<string> failed = new List<string>();

            if (!AssetDatabase.MoveAssetsToTrash(paths.ToArray(), failed))
            {
                Debug.LogError($"{nameof(LocalisationWriter)}::{nameof(OfferToDeleteGoneSheets)} Could not delete: {string.Join(", ", failed)}");
            }
        }

        private static void SavePull(LocalisationMaster master, byte[] workbook)
        {
            string path = GetPullPath(master);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, workbook);
        }

        /// <summary>Where the master's last pull is kept: Library, so it is per machine and never committed.</summary>
        private static string GetPullPath(LocalisationMaster master)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(master));
            string path = $"{PULL_FOLDER}/{guid}.xlsx";

            return path;
        }

        /// <returns>The sheets' assets, which the last pull left in the master's list in the same order.</returns>
        private static LocalisationSheetAsset[] GetPulledSheetAssets(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            LocalisationSheetAsset[] assets  = new LocalisationSheetAsset[sheets.Count];
            bool                     matches = master.SheetAssets != null && master.SheetAssets.Length == sheets.Count;

            for (int i = 0; matches && i < sheets.Count; i++)
            {
                assets[i] = master.SheetAssets[i];
                matches   = assets[i] != null && assets[i].SheetName == sheets[i].SheetName;
            }

            if (!matches)
            {
                throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(GetPulledSheetAssets)} [{master.name}]'s sheet assets no longer match its last pull. Pull again");
            }

            return assets;
        }

        private static string GetSheetsFolder(LocalisationMaster master)
        {
            string folder = $"{GetAssetFolder(AssetDatabase.GetAssetPath(master))}/{SHEETS_FOLDER}";

            return folder;
        }

        /// <summary>The folder an asset path is in, as an asset path: Path's own answer uses backslashes on Windows.</summary>
        private static string GetAssetFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

            return folder;
        }

        private static string GetKeysClassFolder(LocalisationMaster master, LocalisationSheetAsset asset)
        {
            string folder = asset == null || string.IsNullOrEmpty(asset.KeysClassFolderOverride) ? master.DefaultKeysClassFolder : asset.KeysClassFolderOverride;

            return folder;
        }

        /// <summary>A sheet's name names its asset and begins every key, so the pull checks it before either is made.</summary>
        private static void ValidateSheetNames(List<LocalisationSheetContent> sheets)
        {
            HashSet<string> seenSheetNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < sheets.Count; i++)
            {
                string sheetName = sheets[i].SheetName;

                if (!IsValidIdentifier(sheetName))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateSheetNames)} Tab [{sheetName}] is not named as a valid C# identifier, which a sheet's name must be: it begins every key a script writes, and names the sheet's keys class. Start its name with '{SheetParser.SKIPPED_TAB_PREFIX}' if it is not a sheet");
                }

                if (!seenSheetNames.Add(sheetName))
                {
                    throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateSheetNames)} Two sheets are both named [{sheetName}]. Names must be unique: they scope every key and name the generated file");
                }
            }
        }

        private static void ValidateSheets(LocalisationMaster master, List<LocalisationSheetContent> sheets, LocalisationSheetAsset[] assets)
        {
            for (int i = 0; i < sheets.Count; i++)
            {
                LocalisationSheetContent sheet = sheets[i];
                LocalisationSheetAsset   asset = assets[i];

                if (asset.GenerateKeysClass)
                {
                    ValidateKeysClass(master, asset, sheet);
                }

                ReportEmptyValues(sheet.SheetName, sheet);
            }

            ValidateLanguageConsistency(master, sheets);
            ValidateText(sheets);
        }

        /// <summary>
        /// Run every <see cref="ILocalisationTextValidator" /> in the project over every value, and refuse to write
        /// anything if one finds a problem.
        /// </summary>
        private static void ValidateText(List<LocalisationSheetContent> sheets)
        {
            List<ILocalisationTextValidator> validators = new List<ILocalisationTextValidator>();

            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ILocalisationTextValidator>();

            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];

                if (!type.IsAbstract)
                {
                    validators.Add((ILocalisationTextValidator)Activator.CreateInstance(type));
                }
            }

            List<string> problems = new List<string>();

            for (int j = 0; j < sheets.Count; j++)
            {
                LocalisationSheetContent sheet = sheets[j];

                for (int k = 0; k < sheet.Languages.Count; k++)
                {
                    string language = sheet.Languages[k];

                    List<string> values = sheet.Values[language];

                    for (int i = 0; i < values.Count; i++)
                    {
                        for (int m = 0; m < validators.Count; m++)
                        {
                            ILocalisationTextValidator validator = validators[m];

                            validator.Validate(sheet.SheetName, language, sheet.Keys[i], values[i] ?? string.Empty, problems);
                        }
                    }
                }
            }

            if (problems.Count > 0)
            {
                throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateText)} {problems.Count} problem(s) in the sheets' text. Nothing was written.\n{string.Join("\n", problems)}");
            }
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

        private static void ValidateKeysClass(LocalisationMaster master, LocalisationSheetAsset asset, LocalisationSheetContent sheet)
        {
            string namespaceToUse = string.IsNullOrEmpty(asset.NamespaceOverride) ? master.DefaultNamespace : asset.NamespaceOverride;

            if (!IsValidNamespace(namespaceToUse))
            {
                throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateKeysClass)} Namespace [{namespaceToUse}] used by sheet [{sheet.SheetName}] is not a valid C# namespace");
            }

            if (string.IsNullOrEmpty(GetKeysClassFolder(master, asset)))
            {
                throw new InvalidDataException($"{nameof(LocalisationWriter)}::{nameof(ValidateKeysClass)} Sheet [{sheet.SheetName}] has no folder for its keys class. Set the master's {nameof(LocalisationMaster.DefaultKeysClassFolder)}");
            }

            ValidateGeneratedIdentifiers(sheet.SheetName, sheet.Keys);
        }

        private static void ValidateGeneratedIdentifiers(string sheetName, List<string> keys)
        {
            Dictionary<string, string> seen = new Dictionary<string, string>(keys.Count, StringComparer.Ordinal);

            seen.Add("SHEET_NAME", "<generated sheet name constant>");

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];

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
            for (int j = 0; j < sheet.Languages.Count; j++)
            {
                string language = sheet.Languages[j];

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
            ILocalisationBinGenerator localisationBinGenerator = LocalisationBinGeneratorProvider.GetLocalisationBinGenerator((byte)master.Version);
            List<LocalisationBinFile> files                    = localisationBinGenerator.BuildLocalisationBin(sheets);

            if (Directory.Exists(Constants.BasePath))
            {
                Directory.Delete(Constants.BasePath, true);
            }

            Directory.CreateDirectory(Constants.BasePath);

            for (int i = 0; i < files.Count; i++)
            {
                LocalisationBinFile file = files[i];

                string directory = Path.GetDirectoryName(file.Path);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(file.Path, file.Bytes);

                Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteLocalisationBin)} Wrote {file.Path}");
            }
        }

        private static void WriteLocalisationManifest(LocalisationMaster master, List<LocalisationSheetContent> sheets)
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

            string manifestPath = Path.Combine(Constants.BasePath, Constants.MANIFEST_FILE);

            using (FileStream fs = new FileStream(manifestPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(Constants.LocManMagic);
                bw.Write((byte)master.Version);

                for (int i = 0; i < languages.Count; i++)
                {
                    string language = languages[i];

                    bw.Write(Encoding.UTF8.GetBytes(language));
                    bw.Write((byte)0);
                }

                bw.Flush();
            }

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteLocalisationManifest)} Wrote {manifestPath}");
        }

        private static void WriteKeysToClassFiles(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            List<string> unwanted = new List<string>();

            for (int i = 0; i < sheets.Count; i++)
            {
                LocalisationSheetAsset asset = master.SheetAssets[i];

                if (asset.GenerateKeysClass)
                {
                    WriteGeneratedKeysClass(master, asset, sheets[i].Keys);

                    continue;
                }

                string keysClass = GetKeysClassPath(master, asset);

                if (keysClass != null)
                {
                    unwanted.Add(keysClass);
                }
            }

            if (unwanted.Count == 0)
            {
                return;
            }

            List<string> failed = new List<string>();

            if (!AssetDatabase.MoveAssetsToTrash(unwanted.ToArray(), failed))
            {
                Debug.LogError($"{nameof(LocalisationWriter)}::{nameof(WriteKeysToClassFiles)} Could not delete the keys classes of sheets that no longer generate one: {string.Join(", ", failed)}");

                return;
            }

            Debug.Log($"{nameof(LocalisationWriter)}::{nameof(WriteKeysToClassFiles)} Deleted the keys classes of sheets that no longer generate one: {string.Join(", ", unwanted)}");
        }

        /// <returns>The sheet's generated keys class, or null when there is none: no file, or a file this writer did not make.</returns>
        private static string GetKeysClassPath(LocalisationMaster master, LocalisationSheetAsset asset)
        {
            string folder = GetKeysClassFolder(master, asset);

            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(asset.SheetName))
            {
                return null;
            }

            string path = $"{folder}/{asset.SheetName}.cs";

            if (!File.Exists(path))
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(path))
            {
                for (int i = 0; i < 5; i++)
                {
                    string line = reader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    if (line.Trim() == $"{KEYS_CLASS_MARKER}{asset.SheetName}")
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private static void RecordKeysOnSheets(LocalisationMaster master, List<LocalisationSheetContent> sheets)
        {
            for (int i = 0; i < sheets.Count; i++)
            {
                LocalisationSheetAsset asset = master.SheetAssets[i];
                List<string>           keys  = new List<string>(sheets[i].Keys.Count);

                List<string> sheetKeys = sheets[i].Keys;

                for (int j = 0; j < sheetKeys.Count; j++)
                {
                    string key = sheetKeys[j];

                    keys.Add($"{asset.SheetName}{LocalisationBinaryBuilder.KEY_SEPARATOR}{key}");
                }

                asset.SetKeys(keys);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void WriteGeneratedKeysClass(LocalisationMaster master, LocalisationSheetAsset asset, List<string> keys)
        {
            string outFolder = GetKeysClassFolder(master, asset);

            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }

            string namespaceToUse = string.IsNullOrEmpty(asset.NamespaceOverride) ? master.DefaultNamespace : asset.NamespaceOverride;
            string sheetName      = asset.SheetName;

            string             path = Path.Combine(outFolder, $"{sheetName}.cs");
            using StreamWriter sw   = new StreamWriter(path, false, Encoding.UTF8);

            sw.WriteLine($"namespace {namespaceToUse}");
            sw.WriteLine("{");
            sw.WriteLine($"\t{KEYS_CLASS_MARKER}{sheetName}");
            sw.WriteLine("\tpublic static partial class LocalisationKeys");
            sw.WriteLine("\t{");
            sw.WriteLine($"\t\tpublic static class {sheetName}");
            sw.WriteLine("\t\t{");

            sw.WriteLine($"\t\t\t///<summary>{EscapeXmlDoc(sheetName)}</summary>");
            sw.WriteLine($"\t\t\tpublic const string SHEET_NAME = @\"{EscapeVerbatim(sheetName)}\";");

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];

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

            string[] parts = value.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

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

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

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