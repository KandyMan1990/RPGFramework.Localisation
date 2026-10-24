using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    /// <summary>A device in Japanese, and a game shipping French and English.</summary>
    internal sealed class WhenNothingNearTheDeviceLanguageShips
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { Sheets.FRENCH, Sheets.ENGLISH }, new[] { "OK" }, new[] { "D'accord" }, new[] { "OK" }) };

            m_Service = StreamingAssets.Started(StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, sheets), "ja-JP");
        }

        [Test]
        public void ThenTheGameStartsInTheFirstItShips()
        {
            Assert.AreEqual(Sheets.FRENCH, m_Service.CurrentLanguage);
        }
    }
}
