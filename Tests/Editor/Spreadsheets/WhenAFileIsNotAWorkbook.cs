using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;

namespace RPGFramework.Localisation.Tests.Spreadsheets
{
    internal sealed class WhenAFileIsNotAWorkbook
    {
        [Test]
        public void ThenItIsRefused()
        {
            byte[] file = Workbooks.Zip(new Dictionary<string, string> { ["readme.txt"] = "hello" });

            InvalidDataException refused = Assert.Throws<InvalidDataException>(() => XlsxReader.Read(file));

            StringAssert.Contains("xl/workbook.xml", refused.Message);
        }
    }
}
