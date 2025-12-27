using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.LocalisationBinLoader;
using RPGFramework.Localisation.StreamingAssetLoader;
using UnityEngine;

namespace RPGFramework.Localisation
{
    internal class LocalisationSheetSourceProvider : ILocalisationSheetSourceProvider
    {
        private const string MANIFEST_FILE = "manifest.locman";

        private readonly IStreamingAssetLoader                        m_AssetLoader;
        private readonly string                                       m_BasePath;
        private readonly Dictionary<string, ILocalisationSheetSource> m_Cache;

        private ManifestData m_ManifestData;

        public LocalisationSheetSourceProvider()
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            m_AssetLoader = new WebStreamingAssetLoader();
#else
            m_AssetLoader = new FileStreamingAssetLoader();
#endif
            m_BasePath = Utilities.Combine(Application.streamingAssetsPath, "Localisation");
            m_Cache    = new Dictionary<string, ILocalisationSheetSource>();
        }

        async Task<ILocalisationSheetSource> ILocalisationSheetSourceProvider.GetLocalisationSheetSource(string language)
        {
            if (m_Cache.TryGetValue(language, out ILocalisationSheetSource sheetSource))
            {
                return sheetSource;
            }

            await LoadManifest();

            ILocalisationSheetSource source = m_ManifestData.Version switch
                                              {
                                                      1 => new LocalisationSheetSource_Version01(m_AssetLoader, m_BasePath),
                                                      //2 => new LocalisationSheetSource_Version02(m_AssetLoader, m_BasePath),
                                                      _ => throw new VersionNotFoundException($"{nameof(ILocalisationSheetSourceProvider)}::{nameof(ILocalisationSheetSourceProvider.GetLocalisationSheetSource)} version [{m_ManifestData.Version}] is not registered")
                                              };

            m_Cache[language] = source;

            return source;
        }

        async Task<string[]> ILocalisationSheetSourceProvider.GetAllLanguages()
        {
            await LoadManifest();
            return m_ManifestData.Languages;
        }

        private async Task LoadManifest()
        {
            if (m_ManifestData.Version != 0)
            {
                return;
            }

            string path = Utilities.Combine(m_BasePath, MANIFEST_FILE);

            byte[] bytes = await m_AssetLoader.LoadAsync(path);

            if (bytes == null || bytes.Length == 0)
            {
                throw new FileNotFoundException($"{nameof(ILocalisationSheetSourceProvider)}::{nameof(ILocalisationSheetSourceProvider.GetAllLanguages)} Manifest not found");
            }

            m_ManifestData = LocalisationBinFileReader.ReadLocMan(bytes);
        }
    }
}