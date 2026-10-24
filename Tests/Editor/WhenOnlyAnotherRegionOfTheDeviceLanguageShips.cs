using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    /// <summary>A device in French (Canada), and a game shipping German first and French (France).</summary>
    internal sealed class WhenOnlyAnotherRegionOfTheDeviceLanguageShips
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { "de-DE", Sheets.FRENCH }, new[] { "OK" }, new[] { "OK" }, new[] { "D'accord" }) };

            m_Service = StreamingAssets.Started(StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, sheets), "fr-CA");
        }

        [Test]
        public void ThenTheGameStartsInThatRegion()
        {
            Assert.AreEqual(Sheets.FRENCH, m_Service.CurrentLanguage);
        }
    }
}
