using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    internal sealed class WhenAFilePerSheetIsRead
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            FakeStreamingAssetLoader assets = StreamingAssets.Generated(StreamingAssets.FILE_PER_SHEET, Sheets.Three());

            m_Service = StreamingAssets.Service(assets, Sheets.FRENCH);

            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync("Generic"));
        }

        [TestCase("Generic/OK", "D'accord")]
        [TestCase("Generic/Cancel", "Annuler")]
        public void ThenItsEntriesReadBack(string key, string text)
        {
            Assert.AreEqual(text, m_Service.Get(key));
        }
    }
}
