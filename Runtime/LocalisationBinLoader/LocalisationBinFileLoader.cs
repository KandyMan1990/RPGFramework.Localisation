using System.Data;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinFileLoader
    {
        /// <summary>
        /// Loads a locbin for <paramref name="language"/>, falling back to <paramref name="neutralLanguage"/>
        /// when the regional file is absent. Only absence triggers the fallback; IO and network failures
        /// propagate.
        /// </summary>
        internal static async Task<byte[]> LoadFileAsync(string language, string neutralLanguage, string sheetName, byte version)
        {
            string primary = GetPath(language, sheetName, version);

            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            byte[] bytes = await assetLoader.LoadAsync(primary);

            if (bytes != null)
            {
                return bytes;
            }

            string fallback = null;

            if (neutralLanguage != language)
            {
                fallback = GetPath(neutralLanguage, sheetName, version);
                bytes    = await assetLoader.LoadAsync(fallback);

                if (bytes != null)
                {
                    return bytes;
                }
            }

            string attempted = fallback == null ? $"[{primary}]" : $"[{primary}] and [{fallback}]";

            throw version switch
                  {
                      1 => new FileNotFoundException($"{nameof(ILocalisationBinLoader)}::{nameof(ILocalisationBinLoader.LoadSheetAsync)} Missing .locbin for sheet [{sheetName}] language=[{language}], neutral=[{neutralLanguage}]. Tried {attempted}"),
                      2 => new FileNotFoundException($"{nameof(ILocalisationBinLoader)}::{nameof(ILocalisationBinLoader.LoadSheetAsync)} Missing .locbin language=[{language}], neutral=[{neutralLanguage}]. Tried {attempted}"),
                      _ => new VersionNotFoundException($"{nameof(LocalisationBinFileLoader)}::{nameof(LoadFileAsync)} Version [{version}] not registered")
                  };
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