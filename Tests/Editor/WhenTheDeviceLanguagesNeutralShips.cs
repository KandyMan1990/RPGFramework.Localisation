using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    /// <summary>A device in French (Canada), and a game shipping French (France) before plain French.</summary>
    internal sealed class WhenTheDeviceLanguagesNeutralShips
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { Sheets.FRENCH, "fr" }, new[] { "OK" }, new[] { "D'accord" }, new[] { "D'accord" }) };

            m_Service = StreamingAssets.Started(StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, sheets), "fr-CA");
        }

        [Test]
        public void ThenTheGameStartsInTheNeutral()
        {
            Assert.AreEqual("fr", m_Service.CurrentLanguage);
        }
    }
}
