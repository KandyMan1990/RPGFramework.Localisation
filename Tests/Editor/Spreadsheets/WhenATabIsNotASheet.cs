using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;

namespace RPGFramework.Localisation.Tests.Spreadsheets
{
    internal sealed class WhenATabIsNotASheet
    {
        [Test]
        public void ThenOneWhoseNameStartsWithAHashIsLeftOut()
        {
            List<LocalisationSheetContent> sheets = SheetParser.ReadSheets(XlsxReader.Read(Workbooks.Workbook(Workbooks.Generic, Workbooks.Notes, Workbooks.Second)));

            Assert.AreEqual(2, sheets.Count);
            Assert.AreEqual("Second", sheets[1].SheetName);
        }

        [Test]
        public void ThenAnyOtherIsRefusedByNameSayingHowToSkipIt()
        {
            List<SpreadsheetTab> tabs = XlsxReader.Read(Workbooks.Workbook(Workbooks.Generic, new WorkbookTab("Notes", "worksheets/sheet2.xml", Workbooks.NotesRows())));

            InvalidDataException refused = Assert.Throws<InvalidDataException>(() => SheetParser.ReadSheets(tabs));

            StringAssert.Contains("Tab [Notes]", refused.Message);
        }

        [Test]
        public void ThenASpreadsheetWhoseEveryTabIsSkippedIsRefused()
        {
            List<SpreadsheetTab> tabs = XlsxReader.Read(Workbooks.Workbook(new WorkbookTab("#Notes", "worksheets/sheet1.xml", Workbooks.NotesRows())));

            InvalidDataException refused = Assert.Throws<InvalidDataException>(() => SheetParser.ReadSheets(tabs));

            StringAssert.Contains("no sheets", refused.Message);
        }
    }
}
