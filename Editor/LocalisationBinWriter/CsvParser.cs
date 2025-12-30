using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class CsvParser
    {
        internal static List<string[]> ParseCsv(string csv)
        {
            List<string[]> outList = new List<string[]>();

            using StringReader sr = new StringReader(csv);

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                List<string> cells = ParseCsvLine(line);
                outList.Add(cells.ToArray());
            }

            if (outList.Count < 1)
            {
                throw new InvalidDataException($"{nameof(CsvParser)}::{nameof(ParseCsv)} CSV could not be parsed");
            }

            return outList;
        }

        internal static List<string> GetLanguages(string[] header)
        {
            if (header.Length <= 2)
            {
                throw new InvalidDataException($"{nameof(CsvParser)}::{nameof(GetLanguages)} Need language headers starting at column C");
            }

            List<string> languages = new List<string>();
            for (int i = 2; i < header.Length; i++)
            {
                string code = header[i].Trim();

                if (string.IsNullOrEmpty(code))
                {
                    throw new InvalidDataException($"{nameof(CsvParser)}::{nameof(GetLanguages)} Header {i} is empty");
                }

                if (!IsValidCulture(code, out CultureInfo _))
                {
                    throw new InvalidDataException($"{nameof(CsvParser)}::{nameof(GetLanguages)} Language code [{code}] is not valid.  Use a format like 'en-GB' or 'fr-FR'");
                }

                languages.Add(code);
            }

            return languages;
        }

        internal static void GetValues(List<string> languages, List<string[]> rows, out List<string> keys, out List<string> comments, out Dictionary<string, List<string>> perLangValues)
        {
            keys          = new List<string>();
            comments      = new List<string>();
            perLangValues = languages.ToDictionary(l => l, _ => new List<string>());

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
                throw new InvalidDataException($"{nameof(CsvParser)}::{nameof(GetValues)} No keys found");
            }
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