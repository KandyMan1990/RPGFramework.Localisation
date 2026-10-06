using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    /// <summary>
    /// Reads every tab of an .xlsx workbook as rows of text, as the spreadsheet's CSV export gives a tab: a formula
    /// reads as its result, a row ends at its last value, and every row is padded to the widest.
    /// </summary>
    internal static class XlsxReader
    {
        private static readonly XNamespace m_Main          = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace m_Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace m_Package       = "http://schemas.openxmlformats.org/package/2006/relationships";

        internal static List<SpreadsheetTab> Read(byte[] xlsx)
        {
            using MemoryStream stream  = new MemoryStream(xlsx);
            using ZipArchive   archive = new ZipArchive(stream, ZipArchiveMode.Read);

            XDocument                  workbook      = Load(archive, "xl/workbook.xml");
            Dictionary<string, string> worksheets    = ReadWorksheetPaths(Load(archive, "xl/_rels/workbook.xml.rels"));
            List<string>               sharedStrings = ReadSharedStrings(archive);
            XElement                   sheets        = workbook.Root?.Element(m_Main + "sheets");

            if (sheets == null)
            {
                throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(Read)} The workbook lists no tabs");
            }

            List<SpreadsheetTab> tabs = new List<SpreadsheetTab>();

            foreach (XElement sheet in sheets.Elements(m_Main + "sheet"))
            {
                string name = (string)sheet.Attribute("name");
                string id   = (string)sheet.Attribute(m_Relationships + "id");

                if (id == null || !worksheets.TryGetValue(id, out string path))
                {
                    throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(Read)} Tab [{name}] names no worksheet in the workbook");
                }

                tabs.Add(new SpreadsheetTab(name, ReadRows(Load(archive, path), sharedStrings, name)));
            }

            return tabs;
        }

        private static XDocument Load(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path);

            if (entry == null)
            {
                throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(Load)} [{path}] is missing, so this is not an .xlsx workbook");
            }

            using Stream stream = entry.Open();

            XDocument document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);

            return document;
        }

        private static Dictionary<string, string> ReadWorksheetPaths(XDocument relationships)
        {
            Dictionary<string, string> paths = new Dictionary<string, string>();

            foreach (XElement relationship in relationships.Root.Elements(m_Package + "Relationship"))
            {
                string target = (string)relationship.Attribute("Target");

                // A target is relative to the workbook's folder unless it starts at the package's root.
                paths[(string)relationship.Attribute("Id")] = target.StartsWith("/") ? target.Substring(1) : $"xl/{target}";
            }

            return paths;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            List<string> strings = new List<string>();

            if (archive.GetEntry("xl/sharedStrings.xml") == null)
            {
                return strings;
            }

            foreach (XElement item in Load(archive, "xl/sharedStrings.xml").Root.Elements(m_Main + "si"))
            {
                strings.Add(ReadText(item));
            }

            return strings;
        }

        /// <summary>
        /// The text of a shared or inline string: its own <c>t</c>, or its formatted runs' joined. Phonetic guides
        /// (<c>rPh</c>) are not part of the text.
        /// </summary>
        private static string ReadText(XElement item)
        {
            StringBuilder text = new StringBuilder();

            foreach (XElement child in item.Elements())
            {
                if (child.Name == m_Main + "t")
                {
                    text.Append(child.Value);
                }
                else if (child.Name == m_Main + "r")
                {
                    text.Append((string)child.Element(m_Main + "t") ?? string.Empty);
                }
            }

            return text.ToString();
        }

        private static List<string[]> ReadRows(XDocument worksheet, List<string> sharedStrings, string tabName)
        {
            List<List<string>> rows      = new List<List<string>>();
            XElement           sheetData = worksheet.Root?.Element(m_Main + "sheetData");

            if (sheetData != null)
            {
                foreach (XElement row in sheetData.Elements(m_Main + "row"))
                {
                    XAttribute number = row.Attribute("r");
                    int        index  = number == null ? rows.Count : int.Parse(number.Value) - 1;

                    if (index < rows.Count)
                    {
                        throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(ReadRows)} Tab [{tabName}] lists row [{index + 1}] out of order");
                    }

                    // A row with nothing in it is left out of the file.
                    while (rows.Count < index)
                    {
                        rows.Add(new List<string>());
                    }

                    rows.Add(ReadCells(row, sharedStrings, tabName));
                }
            }

            int width = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                width = rows[i].Count > width ? rows[i].Count : width;
            }

            List<string[]> padded = new List<string[]>(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                string[] cells = new string[width];

                for (int j = 0; j < width; j++)
                {
                    cells[j] = j < rows[i].Count ? rows[i][j] : string.Empty;
                }

                padded.Add(cells);
            }

            return padded;
        }

        /// <returns>The row's cells up to its last value. A formatted cell with no value counts as empty.</returns>
        private static List<string> ReadCells(XElement row, List<string> sharedStrings, string tabName)
        {
            List<string> cells = new List<string>();
            int          next  = 0;

            foreach (XElement cell in row.Elements(m_Main + "c"))
            {
                string reference = (string)cell.Attribute("r");
                int    column    = reference == null ? next : ColumnIndex(reference);
                string value     = ReadValue(cell, sharedStrings, tabName);

                if (column < cells.Count)
                {
                    throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(ReadCells)} Tab [{tabName}] lists cell [{reference}] out of order");
                }

                if (value.Length > 0)
                {
                    while (cells.Count < column)
                    {
                        cells.Add(string.Empty);
                    }

                    cells.Add(value);
                }

                next = column + 1;
            }

            return cells;
        }

        private static string ReadValue(XElement cell, List<string> sharedStrings, string tabName)
        {
            string stored = (string)cell.Element(m_Main + "v");
            string value;

            switch ((string)cell.Attribute("t"))
            {
                case "s":
                    int index = stored == null ? -1 : int.Parse(stored);

                    if (index >= sharedStrings.Count)
                    {
                        throw new InvalidDataException($"{nameof(XlsxReader)}::{nameof(ReadValue)} Tab [{tabName}] cell [{(string)cell.Attribute("r")}] names shared string [{index}], and there are [{sharedStrings.Count}]");
                    }

                    value = index < 0 ? string.Empty : sharedStrings[index];
                    break;
                case "inlineStr":
                    XElement inline = cell.Element(m_Main + "is");

                    value = inline == null ? string.Empty : ReadText(inline);
                    break;
                case "b":
                    value = stored == null ? string.Empty : stored == "1" ? "TRUE" : "FALSE";
                    break;
                default:
                    // A number, a date's serial number, a formula's text or an error such as #N/A, as stored.
                    value = stored ?? string.Empty;
                    break;
            }

            return value;
        }

        /// <summary>The zero-based column of a cell reference such as <c>AB12</c>.</summary>
        private static int ColumnIndex(string reference)
        {
            int column = 0;

            for (int i = 0; i < reference.Length && char.IsLetter(reference[i]); i++)
            {
                column = column * 26 + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            }

            return column - 1;
        }
    }
}