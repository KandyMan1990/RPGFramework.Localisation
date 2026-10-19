using System;
using System.IO;
using NUnit.Framework;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    /// <summary>The last sheet cut short: the table of contents still promises bytes that are not there.</summary>
    internal sealed class WhenASheetIsCutShort
    {
        [Test]
        public void ThenItIsRefusedRatherThanReadAsGarbage()
        {
            FakeStreamingAssetLoader assets = StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, Sheets.Three());
            string                   path   = StreamingAssets.LanguageFile(Sheets.ENGLISH);
            byte[]                   whole  = assets.Get(path);
            byte[]                   cut    = new byte[whole.Length - 4];

            Array.Copy(whole, cut, cut.Length);
            assets.Put(path, cut);

            ILocalisationService service = StreamingAssets.Service(assets, Sheets.ENGLISH);

            InvalidDataException refused = Assert.Throws<InvalidDataException>(() => StreamingAssets.Run(service.LoadNewLocalisationDataAsync("Locations")));

            StringAssert.Contains("Locations", refused.Message);
        }
    }
}
