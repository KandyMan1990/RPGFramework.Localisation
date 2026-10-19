using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Tests.Files;

namespace RPGFramework.Localisation.Tests
{
    /// <summary>A game listing a regional language whose text ships only as the neutral one.</summary>
    internal sealed class WhenOnlyTheNeutralLanguagesFileShips
    {
        [Test]
        public void ThenTheRegionalLanguageReadsFromIt()
        {
            List<LocalisationSheetContent> neutral  = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { "en" }, new[] { "OK" }, new[] { "Okay" }) };
            List<LocalisationSheetContent> regional = new List<LocalisationSheetContent> { Sheets.Sheet("Generic", new[] { Sheets.ENGLISH }, new[] { "OK" }, new[] { "Okay" }) };
            FakeStreamingAssetLoader       assets   = new FakeStreamingAssetLoader();

            StreamingAssets.PutFiles(assets, StreamingAssets.FILE_PER_LANGUAGE, neutral);
            StreamingAssets.PutManifest(assets, StreamingAssets.FILE_PER_LANGUAGE, regional);

            ILocalisationService service = StreamingAssets.Service(assets, Sheets.ENGLISH);

            StreamingAssets.Run(service.LoadNewLocalisationDataAsync("Generic"));

            Assert.AreEqual("Okay", service.Get("Generic/OK"));
        }
    }
}
