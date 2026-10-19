using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Localisation.Editor;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.LocalisationBinLoader;

namespace RPGFramework.Localisation.Tests.Files
{
    /// <summary>Files as the editor generates them, put where the runtime looks for them.</summary>
    internal static class StreamingAssets
    {
        internal const byte FILE_PER_SHEET    = 1;
        internal const byte FILE_PER_LANGUAGE = 2;

        internal static FakeStreamingAssetLoader Generated(byte version, List<LocalisationSheetContent> sheets)
        {
            FakeStreamingAssetLoader assets = new FakeStreamingAssetLoader();

            PutFiles(assets, version, sheets);
            PutManifest(assets, version, sheets);

            return assets;
        }

        internal static void PutFiles(FakeStreamingAssetLoader assets, byte version, List<LocalisationSheetContent> sheets)
        {
            ILocalisationBinGenerator generator = LocalisationBinGeneratorFactory.Create(version);
            List<LocalisationBinFile> files     = generator.BuildLocalisationBin(sheets);

            for (int i = 0; i < files.Count; i++)
            {
                assets.Put(files[i].Path, files[i].Bytes);
            }
        }

        internal static void PutManifest(FakeStreamingAssetLoader assets, byte version, List<LocalisationSheetContent> sheets)
        {
            assets.Put(HelperFunctions.CombinePath(Constants.BasePath, Constants.MANIFEST_FILE), LocalisationManifestBuilder.Build(version, sheets));
        }

        /// <summary>Where a file per language for <paramref name="language" /> is kept.</summary>
        internal static string LanguageFile(string language)
        {
            string path = LocalisationBinFileLoader.GetPath(language, null, FILE_PER_LANGUAGE);

            return path;
        }

        /// <summary>A service over the files, in <paramref name="language" />.</summary>
        internal static ILocalisationService Service(FakeStreamingAssetLoader assets, string language)
        {
            ILocalisationService service = new StreamingAssetLocalisation(assets);

            Run(service.InitialiseAsync());
            Run(service.SetCurrentLanguage(language));

            return service;
        }

        /// <summary>The fake's reads complete at once, so a task over them has finished, or thrown, when this returns.</summary>
        internal static void Run(Task task)
        {
            task.GetAwaiter().GetResult();
        }
    }
}
