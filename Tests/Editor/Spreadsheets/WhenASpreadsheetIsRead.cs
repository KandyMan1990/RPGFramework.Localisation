using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;

namespace RPGFramework.Localisation.Tests.Spreadsheets
{
    internal sealed class WhenASpreadsheetIsRead
    {
        private List<SpreadsheetTab> m_Tabs;
        private List<string[]>       m_Rows;

        [SetUp]
        public void SetUp()
        {
            m_Tabs = XlsxReader.Read(Workbooks.Workbook(Workbooks.Generic, Workbooks.Notes, Workbooks.Second));
            m_Rows = m_Tabs[0].Rows;
        }

        [Test]
        public void ThenItsTabsAreReadInItsOrderWhereverTheirFilesAreNamedFrom()
        {
            Assert.AreEqual(3, m_Tabs.Count);
            Assert.AreEqual("Generic", m_Tabs[0].Name);
            Assert.AreEqual("#Notes", m_Tabs[1].Name);
            Assert.AreEqual("Second", m_Tabs[2].Name);
        }

        [Test]
        public void ThenARowEndsAtItsLastValueAndEveryRowIsPaddedToTheWidest()
        {
            Assert.AreEqual(5, m_Rows.Count);
            Assert.AreEqual(4, m_Rows[0].Length);
            Assert.AreEqual(4, m_Rows[1].Length);
            Assert.AreEqual("fr-FR", m_Rows[0][3]);
        }

        [Test]
        public void ThenSharedAndInlineStringsAndAFormulasResultReadAsTheirText()
        {
            Assert.AreEqual("OK", m_Rows[1][0]);
            Assert.AreEqual("Runs", m_Rows[3][0]);
            Assert.AreEqual("D'accord", m_Rows[1][3]);
        }

        [Test]
        public void ThenACellOrRowTheFileLeavesOutReadsAsEmpty()
        {
            Assert.AreEqual(string.Empty, m_Rows[1][1]);
            Assert.AreEqual(string.Empty, m_Rows[2][0]);
        }

        [Test]
        public void ThenFormattedRunsReadJoinedWithoutAPhoneticGuide()
        {
            Assert.AreEqual("Bold then plain", m_Rows[3][2]);
        }

        [Test]
        public void ThenSpacesAndLineBreaksInACellSurvive()
        {
            Assert.AreEqual("  two lines\nkept  ", m_Rows[4][2]);
        }

        [Test]
        public void ThenATrueOrFalseCellAndANumberReadAsAnExportShowsThem()
        {
            Assert.AreEqual("TRUE", m_Rows[3][3]);
            Assert.AreEqual("12.5", m_Rows[4][3]);
        }
    }
}
