using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RPGFramework.Localisation.Tests.Spreadsheets
{
    /// <summary>
    /// .xlsx files shaped as Google's export is: shared and inline strings, formulas with their results, formatted empty
    /// cells to column Z, and relationship ids out of step with the tabs.
    /// </summary>
    internal static class Workbooks
    {
        private const string MAIN          = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RELATIONSHIPS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        internal const string STRINGS = "<si><t>Key</t></si><si><t>Comments</t></si><si><t>en-GB</t></si><si><t>fr-FR</t></si><si><t>OK</t></si>"
                                      + "<si><r><t>Bold </t></r><r><t>then plain</t></r><rPh><t>guide</t></rPh></si><si><t xml:space=\"preserve\">  two lines\nkept  </t></si>";

        internal static readonly WorkbookTab Generic = new WorkbookTab("Generic", "worksheets/sheet1.xml",
            Rows("<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>2</v></c><c r=\"D1\" t=\"s\"><v>3</v></c><c r=\"E1\" s=\"3\"/><c r=\"Z1\" s=\"3\"/></row>"
               + "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>4</v></c><c r=\"C2\" t=\"s\"><v>4</v></c><c r=\"D2\" t=\"str\"><f>GOOGLETRANSLATE(C2)</f><v>D'accord</v></c></row>"
               + "<row r=\"4\"><c r=\"A4\" t=\"inlineStr\"><is><t>Runs</t></is></c><c r=\"C4\" t=\"s\"><v>5</v></c><c r=\"D4\" t=\"b\"><v>1</v></c></row>"
               + "<row r=\"5\"><c r=\"A5\" t=\"inlineStr\"><is><t>Lines</t></is></c><c r=\"C5\" t=\"s\"><v>6</v></c><c r=\"D5\"><v>12.5</v></c></row>"));

        internal static readonly WorkbookTab Notes = new WorkbookTab("#Notes", "worksheets/sheet2.xml", NotesRows());

        internal static readonly WorkbookTab Second = new WorkbookTab("Second", "/xl/worksheets/sheet3.xml",
            Rows("<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>2</v></c></row>"
               + "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>4</v></c><c r=\"C2\" t=\"s\"><v>4</v></c></row>"));

        internal static string NotesRows()
        {
            string rows = Rows("<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Not a sheet</t></is></c></row>");

            return rows;
        }

        internal static byte[] Workbook(params WorkbookTab[] tabs)
        {
            StringBuilder              workbook      = new StringBuilder($"<workbook xmlns=\"{MAIN}\" xmlns:r=\"{RELATIONSHIPS}\"><sheets>");
            StringBuilder              relationships = new StringBuilder("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            Dictionary<string, string> entries       = new Dictionary<string, string> { ["xl/sharedStrings.xml"] = $"<sst xmlns=\"{MAIN}\">{STRINGS}</sst>" };

            for (int i = 0; i < tabs.Length; i++)
            {
                string id = "rId" + (i + 5);

                workbook.Append($"<sheet name=\"{tabs[i].Name}\" sheetId=\"{i + 1}\" r:id=\"{id}\"/>");
                relationships.Append($"<Relationship Id=\"{id}\" Type=\"{RELATIONSHIPS}/worksheet\" Target=\"{tabs[i].Target}\"/>");

                entries[tabs[i].Target.StartsWith("/") ? tabs[i].Target.Substring(1) : "xl/" + tabs[i].Target] = tabs[i].Xml;
            }

            entries["xl/workbook.xml"]            = workbook.Append("</sheets></workbook>").ToString();
            entries["xl/_rels/workbook.xml.rels"] = relationships.Append("</Relationships>").ToString();

            byte[] file = Zip(entries);

            return file;
        }

        internal static byte[] Zip(Dictionary<string, string> entries)
        {
            using MemoryStream stream = new MemoryStream();

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (KeyValuePair<string, string> entry in entries)
                {
                    using StreamWriter writer = new StreamWriter(archive.CreateEntry(entry.Key).Open(), new UTF8Encoding(false));

                    writer.Write(entry.Value);
                }
            }

            byte[] file = stream.ToArray();

            return file;
        }

        private static string Rows(string rows)
        {
            string sheet = $"<worksheet xmlns=\"{MAIN}\"><sheetData>{rows}</sheetData></worksheet>";

            return sheet;
        }
    }
}
