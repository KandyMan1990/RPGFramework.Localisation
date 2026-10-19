using System.IO;
using NUnit.Framework;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    internal sealed class WhenALanguagesFileHoldsAnotherLanguage
    {
        [Test]
        public void ThenItIsRefused()
        {
            FakeStreamingAssetLoader assets = StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, Sheets.Three());

            assets.Put(StreamingAssets.LanguageFile(Sheets.FRENCH), assets.Get(StreamingAssets.LanguageFile(Sheets.ENGLISH)));

            ILocalisationService service = StreamingAssets.Service(assets, Sheets.FRENCH);

            InvalidDataException refused = Assert.Throws<InvalidDataException>(() => StreamingAssets.Run(service.LoadNewLocalisationDataAsync("Generic")));

            StringAssert.Contains(Sheets.FRENCH, refused.Message);
        }
    }
}
