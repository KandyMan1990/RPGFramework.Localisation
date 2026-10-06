using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class SheetParser
    {
        /// <summary>
        /// A tab whose name starts with this is not a sheet — notes, a glossary — and is skipped. It cannot begin a C#
        /// identifier, so no sheet can be mistaken for one.
        /// </summary>
        internal const string SKIPPED_TAB_PREFIX = "#";

        /// <summary>
        /// The tabs that are sheets, read into keys and each language's values. Every tab but a skipped one must parse.
        /// </summary>
        internal static List<LocalisationSheetContent> ReadSheets(List<SpreadsheetTab> tabs)
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent>(tabs.Count);

            for (int i = 0; i < tabs.Count; i++)
            {
                SpreadsheetTab tab = tabs[i];

                if (tab.Name.StartsWith(SKIPPED_TAB_PREFIX, StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    if (tab.Rows.Count == 0)
                    {
                        throw new InvalidDataException("It is empty");
                    }

                    List<string> languages = GetLanguages(tab.Rows[0]);

                    GetValues(languages, tab.Rows, out List<string> keys, out List<string> _, out Dictionary<string, List<string>> values);

                    sheets.Add(new LocalisationSheetContent(tab.Name, languages, keys, values));
                }
                catch (InvalidDataException e)
                {
                    throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(ReadSheets)} Tab [{tab.Name}] does not read as a sheet: {e.Message}. Start its name with '{SKIPPED_TAB_PREFIX}' if it is not one", e);
                }
            }

            if (sheets.Count == 0)
            {
                throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(ReadSheets)} The spreadsheet has no sheets: every tab's name starts with '{SKIPPED_TAB_PREFIX}'");
            }

            return sheets;
        }

        /// <summary>
        /// Reads the language codes from the header row. Columns A and B are the key and its comment; languages
        /// start at column C. Whatever a sheet declares is what the game ships.
        /// </summary>
        internal static List<string> GetLanguages(string[] header)
        {
            if (header.Length <= 2)
            {
                throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(GetLanguages)} Need language headers starting at column C");
            }

            List<string>    languages = new List<string>();
            HashSet<string> seen      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 2; i < header.Length; i++)
            {
                string code = header[i].Trim();

                if (string.IsNullOrEmpty(code))
                {
                    throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(GetLanguages)} Header {i} is empty");
                }

                if (!seen.Add(code))
                {
                    throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(GetLanguages)} Language column [{code}] appears more than once in the header");
                }

                if (!IsWellFormedCulture(code))
                {
                    throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(GetLanguages)} Language code [{code}] is not valid.  Use a format like 'en-GB' or 'fr-FR'");
                }

                languages.Add(code);
            }

            return languages;
        }

        internal static void GetValues(List<string> languages, List<string[]> rows, out List<string> keys, out List<string> comments, out Dictionary<string, List<string>> perLangValues)
        {
            keys          = new List<string>();
            comments      = new List<string>();
            perLangValues = new Dictionary<string, List<string>>(languages.Count, StringComparer.Ordinal);

            for (int i = 0; i < languages.Count; i++)
            {
                string language = languages[i];

                perLangValues.Add(language, new List<string>());
            }

            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];

                if (row.Length == 0)
                {
                    continue;
                }

                string key     = row[0].Trim();
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
                throw new InvalidDataException($"{nameof(SheetParser)}::{nameof(GetValues)} No keys found");
            }
        }

        private static bool IsWellFormedCulture(string code)
        {
            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(code);

                return !string.IsNullOrEmpty(culture.Name) && string.Equals(culture.Name, code, StringComparison.OrdinalIgnoreCase);
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}