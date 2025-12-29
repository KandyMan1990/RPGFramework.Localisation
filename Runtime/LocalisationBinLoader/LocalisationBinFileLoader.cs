using System.Data;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinFileLoader
    {
        internal static async Task<byte[]> LoadFileAsync(string language, string neutralLanguage, string sheetName, byte version)
        {
            string primary  = GetPath(language,        sheetName, version);
            string fallback = GetPath(neutralLanguage, sheetName, version);

            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            byte[] bytes = await assetLoader.LoadAsync(primary) ?? await assetLoader.LoadAsync(fallback);

            if (bytes == null)
            {
                switch (version)
                {
                    case 1:
                        throw new FileNotFoundException($"{nameof(ILocalisationBinLoader)}::{nameof(ILocalisationBinLoader.LoadSheetAsync)} Missing .locbin for sheet [{sheetName}] language=[{language}], neutral=[{neutralLanguage}]");
                    case 2:
                        throw new FileNotFoundException($"{nameof(ILocalisationBinLoader)}::{nameof(ILocalisationBinLoader.LoadSheetAsync)} Missing .locbin language=[{language}], neutral=[{neutralLanguage}]");
                }
            }

            return bytes;
        }

        private static string GetPath(string language, string sheetName, byte version)
        {
            return version switch
                   {
                           1 => HelperFunctions.CombinePath(Constants.BasePath, language, $"{sheetName}.locbin"),
                           2 => HelperFunctions.CombinePath(Constants.BasePath, $"{language}.locbin"),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinFileLoader)}::{nameof(GetPath)} Version [{version}] not registered")
                   };
        }
    }
}