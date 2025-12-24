using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.Editor.LocBinWriter
{
    /// <summary>
    /// Generates per-language .locbin files and a generated C# key class from a Google Sheet (CSV export).
    /// Layout:
    /// Column A: key
    /// Column B: comment
    /// Column C+: languages (headers in row 1)
    /// </summary>
    public class LocBinWriter_Version01 : ILocBinWriter
    {
        private const byte VERSION = 1;

        void ILocBinWriter.Generate(LocalisationSheetAsset asset)
        {
            if (asset == null || asset.Master == null)
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Asset/Master is null");
                return;
            }

            string csvUrl = BuildCsvUrl(asset.Master, asset);
            if (string.IsNullOrEmpty(csvUrl))
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Could not build CSV URL");
                return;
            }

            string csv = FetchCsvSync(csvUrl);
            if (string.IsNullOrEmpty(csv))
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Failed to fetch CSV");
                return;
            }

            List<string[]> rows = ParseCsv(csv);
            if (rows == null || rows.Count < 1)
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} CSV parse produced no rows");
                return;
            }

            string[] header = rows[0];
            if (header.Length <= 2)
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Need language headers starting at column C");
                return;
            }

            List<string> languages = new List<string>();
            for (int i = 2; i < header.Length; i++)
            {
                string code = header[i].Trim();

                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Header {i} is empty");
                    return;
                }

                if (!IsValidCulture(code, out CultureInfo _))
                {
                    Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Language code [{code}] is not valid.  Use a format like 'en-GB' or 'fr-FR'");
                    return;
                }

                languages.Add(code);
            }

            List<string> keys     = new List<string>();
            List<string> comments = new List<string>();

            Dictionary<string, List<string>> perLangValues = languages.ToDictionary(l => l, _ => new List<string>());

            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];

                if (row.Length == 0)
                {
                    continue;
                }

                string key     = row.Length >= 1 ? row[0].Trim() : null;
                string comment = row.Length >= 2 ? row[1].Trim() : string.Empty;

                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                keys.Add(key);
                comments.Add(comment);

                for (int j = 0; j < languages.Count; j++)
                {
                    int    colIdx = 2 + j;
                    string cell   = colIdx < row.Length ? row[colIdx] : string.Empty;

                    perLangValues[languages[j]].Add(cell ?? string.Empty);
                }
            }

            if (keys.Count == 0)
            {
                Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} No keys found");
                return;
            }

            string streamingBase     = asset.Master.StreamingAssetsBase ?? "Localisation";
            string streamingFullBase = Path.Combine(Application.streamingAssetsPath, streamingBase);

            if (Directory.Exists(streamingFullBase))
            {
                Directory.Delete(streamingFullBase, true);
            }

            Directory.CreateDirectory(streamingFullBase);

            foreach (string lang in languages)
            {
                string folderPath = Path.Combine(streamingFullBase, lang);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                List<string> values   = perLangValues[lang];
                string       filePath = Path.Combine(folderPath, $"{asset.SheetName}.locbin");

                WriteLocBin(lang, filePath, keys, values);
                Debug.Log($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Wrote {filePath}");
            }

            WriteGeneratedKeysClass(asset, keys);

            AssetDatabase.Refresh();

            Debug.Log($"{nameof(LocBinWriter_Version01)}::{nameof(ILocBinWriter.Generate)} Done");
        }

        private static void WriteLocBin(string language, string filePath, List<string> keys, List<string> values)
        {
            if (keys.Count != values.Count)
            {
                throw new ArgumentException("Keys/Values count mismatch");
            }

            MemoryStream stringTable = new MemoryStream();
            BinaryWriter writer      = new BinaryWriter(stringTable, Encoding.UTF8, leaveOpen: true);
            int[]        offsets     = new int[keys.Count];

            for (int i = 0; i < values.Count; i++)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(values[i] ?? string.Empty);
                offsets[i] = (int)stringTable.Position;

                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            writer.Flush();

            byte[] tableBytes = stringTable.ToArray();

            KeyPair[] pairs = new KeyPair[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                ulong hash = Fnv1a64.Hash(keys[i]);
                pairs[i] = new KeyPair(hash, offsets[i]);
            }

            Array.Sort(pairs, (a, b) => a.Hash.CompareTo(b.Hash));

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(Constants.MAGIC);
                bw.Write(VERSION);
                bw.Write((byte)Encoding.UTF8.GetByteCount(language));
                bw.Write(Encoding.UTF8.GetBytes(language));
                bw.Write(pairs.Length);

                for (int i = 0; i < pairs.Length; i++)
                {
                    bw.Write(pairs[i].Hash);
                    bw.Write(pairs[i].Offset);
                }

                bw.Write(tableBytes);
                bw.Flush();
            }
        }

        private readonly struct KeyPair
        {
            public ulong Hash   { get; }
            public int   Offset { get; }

            public KeyPair(ulong hash, int offset)
            {
                Hash   = hash;
                Offset = offset;
            }
        }

        private static void WriteGeneratedKeysClass(LocalisationSheetAsset asset, List<string> keys)
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

            string namespaceToUse = string.IsNullOrEmpty(asset.NamespaceOverride) ? asset.Master.DefaultNamespace : asset.NamespaceOverride;

            string path = Path.Combine(outFolder, $"{asset.SheetName}.cs");
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine($"namespace {namespaceToUse}");
                sw.WriteLine("{");
                sw.WriteLine($"\t// Auto generated keys for sheet: {asset.SheetName}");
                sw.WriteLine($"\tpublic static partial class LocalisationKeys");
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
            }

            Debug.Log($"{nameof(LocBinWriter_Version01)}::{nameof(WriteGeneratedKeysClass)} Generated keys class at {path}");
        }

        private static string BuildCsvUrl(LocalisationMaster master, LocalisationSheetAsset asset)
        {
            string id  = master.SheetId;
            string gid = asset.Gid;
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Sheet id is null or empty");
            }

            if (string.IsNullOrEmpty(gid))
            {
                throw new ArgumentException("Gid is null or empty");
            }

            if (id.Contains("docs.google.com"))
            {
                const string token = "/d/";
                int          idx   = id.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    idx += token.Length;
                    string rest = id.Substring(idx);
                    int    end  = rest.IndexOf('/');
                    id = end >= 0 ? rest.Substring(0, end) : rest;
                }
            }

            string csvUrl = $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}";

            return csvUrl;
        }

        private static string FetchCsvSync(string url)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation op = req.SendWebRequest();

                while (!op.isDone)
                {
                }

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"{nameof(LocBinWriter_Version01)}::{nameof(FetchCsvSync)} Fetch error: {req.error}");
                    return null;
                }

                return req.downloadHandler.text;
            }
        }

        private static List<string[]> ParseCsv(string csv)
        {
            List<string[]> outList = new List<string[]>();

            using (StringReader sr = new StringReader(csv))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    List<string> cells = ParseCsvLine(line);
                    outList.Add(cells.ToArray());
                }
            }

            return outList;
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string>  result   = new List<string>();
            bool          inQuotes = false;
            StringBuilder sb       = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            result.Add(sb.ToString());

            return result;
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

        private static bool IsValidCulture(string code, out CultureInfo culture)
        {
            try
            {
                culture = CultureInfo.GetCultureInfo(code);

                return true;

            }
            catch
            {
                culture = null;

                return false;
            }
        }
    }
}