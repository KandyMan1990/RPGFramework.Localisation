using NUnit.Framework;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    internal sealed class WhenSheetsOfAFilePerLanguageAreLoaded
    {
        private ILocalisationService m_Service;

        [SetUp]
        public void SetUp()
        {
            FakeStreamingAssetLoader assets = StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, Sheets.Three());

            m_Service = StreamingAssets.Service(assets, Sheets.ENGLISH);
        }

        [Test]
        public void ThenOneReadsAloneByItsOffset()
        {
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync("TestField"));

            Assert.AreEqual("Héllo wörld — ✓", m_Service.Get("TestField/Test"));
        }

        [Test]
        public void ThenTextOutsideAsciiSurvives()
        {
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync("TestField"));

            Assert.AreEqual("日本語のテキスト", m_Service.Get("TestField/Greeting"));
        }

        [Test]
        public void ThenASheetHoldsOnlyItsOwnKeys()
        {
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync("TestField"));

            Assert.IsFalse(m_Service.TryGet("Generic/OK", out string _));
        }

        [Test]
        public void ThenSeveralReadTogether()
        {
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync(new[] { "Locations", "Generic" }));

            Assert.AreEqual("Harbour Town", m_Service.Get("Locations/Harbour"));
            Assert.AreEqual("Cancel", m_Service.Get("Generic/Cancel"));
        }

        [Test]
        public void ThenEverySheetTheFileHoldsLoads()
        {
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync(new[] { "Generic", "TestField", "Locations" }));

            Assert.AreEqual("OK", m_Service.Get("Generic/OK"));
            Assert.AreEqual("Héllo wörld — ✓", m_Service.Get("TestField/Test"));
            Assert.AreEqual("Harbour Town", m_Service.Get("Locations/Harbour"));
        }
    }
}
