using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.LocalisationBinLoader;

namespace RPGFramework.Localisation
{
    internal class LocalisationSheetSource_Version01 : ILocalisationSheetSource
    {
        private readonly IStreamingAssetLoader m_AssetLoader;
        private readonly string                m_BasePath;

        internal LocalisationSheetSource_Version01(IStreamingAssetLoader assetLoader, string basePath)
        {
            m_AssetLoader = assetLoader;
            m_BasePath    = basePath;
        }

        async Task<LocalisationData> ILocalisationSheetSource.LoadSheetAsync(string language, string sheetName)
        {
            string neutral = Utilities.GetNeutralLanguage(language);

            string primary  = Utilities.Combine(m_BasePath, language, $"{sheetName}.locbin");
            string fallback = Utilities.Combine(m_BasePath, neutral,  $"{sheetName}.locbin");

            byte[] bytes = await m_AssetLoader.LoadAsync(primary) ?? await m_AssetLoader.LoadAsync(fallback);

            if (bytes == null)
            {
                throw new FileNotFoundException($"{nameof(ILocalisationSheetSource)}::{nameof(ILocalisationSheetSource.LoadSheetAsync)} Missing .locbin for sheet [{sheetName}] (language=[{language}], neutral=[{neutral}]");
            }

            return LocalisationBinFileReader.ReadLocBin(bytes, language, neutral);
        }
    }
}