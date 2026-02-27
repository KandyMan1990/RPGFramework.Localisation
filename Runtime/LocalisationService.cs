using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.LocalisationBinLoader;
using RPGFramework.Localisation.Manifest;

namespace RPGFramework.Localisation
{
    public sealed class LocalisationService : ILocalisationService
    {
        event Action<string> ILocalisationService.OnLanguageChanged
        {
            add => m_OnLanguageChanged += value;
            remove => m_OnLanguageChanged -= value;
        }

        string ILocalisationService.CurrentLanguage => m_CurrentLanguage;

        private event Action<string> m_OnLanguageChanged;

        private readonly Dictionary<string, LocalisationData> m_LoadedSheets;
        private readonly ILocalisationService                 m_LocalisationService;
        private readonly Task                                 m_ManifestTask;

        private string                 m_CurrentLanguage;
        private ManifestData           m_Manifest;
        private ILocalisationBinLoader m_LocalisationBinLoader;

        public LocalisationService()
        {
            m_CurrentLanguage     = "en-GB"; //TODO: this should default to system language and fallback to English
            m_LoadedSheets        = new Dictionary<string, LocalisationData>();
            m_LocalisationService = this;
            m_ManifestTask        = LoadManifestAsync();
        }

        async Task ILocalisationService.SetCurrentLanguage(string language)
        {
            if (language == m_CurrentLanguage)
            {
                return;
            }

            await m_ManifestTask;

            if (Array.IndexOf(m_Manifest.Languages, language) < 0)
            {
                throw new InvalidDataException($"{nameof(ILocalisationService)}::{nameof(ILocalisationService.SetCurrentLanguage)} Language [{language}] is not supported");
            }

            string[] sheetNames = m_LoadedSheets.Keys.ToArray();

            m_LocalisationService.UnloadAllLocalisationData();

            m_CurrentLanguage = language;

            await m_LocalisationService.LoadNewLocalisationDataAsync(sheetNames);

            m_OnLanguageChanged?.Invoke(m_CurrentLanguage);
        }

        async Task ILocalisationService.LoadNewLocalisationDataAsync(string sheetName)
        {
            if (m_LoadedSheets.ContainsKey(sheetName))
            {
                return;
            }

            await LoadManifestAsync();

            LocalisationData data = await m_LocalisationBinLoader.LoadSheetAsync(m_CurrentLanguage, sheetName);

            m_LoadedSheets.Add(sheetName, data);
        }

        async Task ILocalisationService.LoadNewLocalisationDataAsync(string[] sheetNames)
        {
            List<string> sheetsToLoad = new List<string>(sheetNames);

            for (int i = 0; i < sheetNames.Length; i++)
            {
                if (m_LoadedSheets.ContainsKey(sheetNames[i]))
                {
                    sheetsToLoad.Remove(sheetNames[i]);
                }
            }

            if (sheetsToLoad.Count == 0)
            {
                return;
            }

            await LoadManifestAsync();

            LocalisationData[] data = await m_LocalisationBinLoader.LoadSheetsAsync(m_CurrentLanguage, sheetsToLoad.ToArray());

            for (int i = 0; i < data.Length; i++)
            {
                m_LoadedSheets.Add(sheetsToLoad[i], data[i]);
            }
        }

        void ILocalisationService.UnloadLocalisationData(string sheetName)
        {
            m_LoadedSheets.Remove(sheetName, out LocalisationData _);
        }

        void ILocalisationService.UnloadLocalisationData(string[] sheetNames)
        {
            for (int i = 0; i < sheetNames.Length; i++)
            {
                m_LocalisationService.UnloadLocalisationData(sheetNames[i]);
            }
        }

        void ILocalisationService.UnloadAllLocalisationData()
        {
            m_LoadedSheets.Clear();
        }

        async Task<string[]> ILocalisationService.GetAllLanguages()
        {
            await m_ManifestTask;

            return m_Manifest.Languages;
        }

        string ILocalisationService.Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return $"MISSING KEY [{key}]";
            }

            if (!TryParseKey(key, out string sheetName, out string keyValue))
            {
                return $"MISSING KEY [{key}]";
            }

            if (!m_LoadedSheets.TryGetValue(sheetName, out LocalisationData localisationData))
            {
                return $"MISSING SHEET [{sheetName}]";
            }

            ulong hash  = Fnv1a64.Hash(keyValue);
            int   index = BinarySearch(localisationData.Hashes, hash);

            if (index < 0)
            {
                return $"MISSING KEY [{key}] IN SHEET [{sheetName}]";
            }

            string str = ReadString(localisationData.StringTable, localisationData.Offsets[index]);

            if (str == null)
            {
                return $"MISSING VALUE FOR [{key}] IN SHEET [{sheetName}]";
            }

            return str;
        }

        string ILocalisationService.Get(ulong key)
        {
            foreach (KeyValuePair<string, LocalisationData> kvp in m_LoadedSheets)
            {
                int index = BinarySearch(kvp.Value.Hashes, key);

                if (index < 0)
                {
                    continue;
                }

                string str = ReadString(kvp.Value.StringTable, kvp.Value.Offsets[index]);

                if (str == null)
                {
                    return $"MISSING VALUE FOR [{key}] IN SHEET [{kvp.Key}]";
                }

                return str;
            }

            return $"[{key}] MISSING";
        }

        private async Task LoadManifestAsync()
        {
            m_Manifest              ??= await ManifestProvider.GetManifestAsync();
            m_LocalisationBinLoader ??= LocalisationBinLoaderProvider.Get(m_Manifest.Version);
        }

        private static int BinarySearch(ulong[] arr, ulong target)
        {
            int lo = 0;
            int hi = arr.Length - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;

                if (arr[mid] == target)
                {
                    return mid;
                }

                if (arr[mid] < target)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return -1;
        }

        private static string ReadString(byte[] table, int offset)
        {
            if (table == null)
            {
                return null;
            }

            if (offset < 0 || offset + 4 > table.Length)
            {
                return null;
            }

            int length = BitConverter.ToInt32(table, offset);

            if (length < 0 || offset + 4 + length > table.Length)
            {
                return null;
            }

            string str = Encoding.UTF8.GetString(table, offset + 4, length);

            return str;
        }

        private static bool TryParseKey(string key, out string sheetName, out string keyValue)
        {
            sheetName = null;
            keyValue  = null;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            int slash = key.IndexOf('/');

            if (slash <= 0 || slash == key.Length - 1)
            {
                return false;
            }

            sheetName = key[..slash];
            keyValue  = key[(slash + 1)..];

            return true;
        }
    }
}