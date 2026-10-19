using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    internal sealed class WhenASheetTheFileDoesNotHoldIsLoaded
    {
        [Test]
        public void ThenItIsRefusedByName()
        {
            FakeStreamingAssetLoader assets  = StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE, Sheets.Three());
            ILocalisationService     service = StreamingAssets.Service(assets, Sheets.ENGLISH);

            KeyNotFoundException refused = Assert.Throws<KeyNotFoundException>(() => StreamingAssets.Run(service.LoadNewLocalisationDataAsync("Missing")));

            StringAssert.Contains("Missing", refused.Message);
        }
    }
}
