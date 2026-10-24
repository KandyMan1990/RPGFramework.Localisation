using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    internal sealed class WhenTheDeviceLanguageShips
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { Sheets.ENGLISH, Sheets.FRENCH }, new[] { "OK" }, new[] { "OK" }, new[] { "D'accord" }) };

            m_Service = StreamingAssets.Started(StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, sheets), Sheets.FRENCH);
        }

        [Test]
        public void ThenTheGameStartsInIt()
        {
            Assert.AreEqual(Sheets.FRENCH, m_Service.CurrentLanguage);
        }
    }
}
