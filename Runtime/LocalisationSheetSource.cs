using System;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.LocalisationBinLoader;
using RPGFramework.Localisation.StreamingAssetLoader;
using UnityEngine;

namespace RPGFramework.Localisation
{
    internal class LocalisationSheetSource : ILocalisationSheetSource
    {
        private const string MANIFEST_FILE = "manifest.locman";

        private readonly IStreamingAssetLoader m_AssetLoader;
        private readonly string                m_BasePath;

        internal LocalisationSheetSource()
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            m_AssetLoader = new WebStreamingAssetLoader();
#else
            m_AssetLoader = new FileStreamingAssetLoader();
#endif
            m_BasePath = Combine(Application.streamingAssetsPath, "Localisation");
        }

        async Task<string[]> ILocalisationSheetSource.GetAllLanguages()
        {
            string path = Combine(m_BasePath, MANIFEST_FILE);

            byte[] bytes = await m_AssetLoader.LoadAsync(path);

            if (bytes == null || bytes.Length == 0)
            {
                return Array.Empty<string>();
            }

            return LocalisationBinFileReader.ReadLocMan(bytes);
        }

        async Task<LocalisationData> ILocalisationSheetSource.LoadSheetAsync(string language, string sheetName)
        {
            string neutral = GetNeutral(language);

            string primary  = Combine(m_BasePath, language, $"{sheetName}.locbin");
            string fallback = Combine(m_BasePath, neutral,  $"{sheetName}.locbin");

            byte[] bytes = await m_AssetLoader.LoadAsync(primary) ?? await m_AssetLoader.LoadAsync(fallback);

            if (bytes == null)
            {
                throw new FileNotFoundException($"{nameof(ILocalisationSheetSource)}::{nameof(ILocalisationSheetSource.LoadSheetAsync)} Missing .locbin for sheet [{sheetName}] (language=[{language}], neutral=[{neutral}]");
            }

            return LocalisationBinFileReader.ReadLocBin(bytes, language, neutral);
        }

        void ILocalisationSheetSource.UnloadSheet(string sheetName)
        {
            // noop
        }

        private static string GetNeutral(string language)
        {
            int index = language.IndexOfAny(new[]
                                            {
                                                    '-',
                                                    '_'
                                            });

            return index > 0 ? language[..index] : language;
        }

        private static string Combine(params string[] parts)
        {
#if UNITY_ANDROID || UNITY_WEBGL
            return string.Join('/', parts.Select(p => p.Trim('/')));
#else
            return Path.Combine(parts);
#endif
        }
    }
}