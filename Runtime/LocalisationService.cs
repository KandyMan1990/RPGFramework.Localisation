using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation
{
    public class LocalisationService : ILocalisationService
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
        private readonly ILocalisationSheetSourceProvider     m_LocalisationSheetSourceProvider;

        private string m_CurrentLanguage;

        public LocalisationService()
        {
            m_LocalisationSheetSourceProvider = new LocalisationSheetSourceProvider();
            m_LoadedSheets                    = new Dictionary<string, LocalisationData>();
            m_CurrentLanguage                 = "en-GB"; //TODO: this should default to system language and fallback to English
            m_LocalisationService             = this;
        }

        async Task ILocalisationService.SetCurrentLanguage(string language)
        {
            if (language == m_CurrentLanguage)
            {
                return;
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

            ILocalisationSheetSource source = await m_LocalisationSheetSourceProvider.GetLocalisationSheetSource(m_CurrentLanguage);

            LocalisationData data = await source.LoadSheetAsync(m_CurrentLanguage, sheetName);

            m_LoadedSheets.Add(sheetName, data);
        }

        async Task ILocalisationService.LoadNewLocalisationDataAsync(string[] sheetNames)
        {
            for (int i = 0; i < sheetNames.Length; i++)
            {
                await m_LocalisationService.LoadNewLocalisationDataAsync(sheetNames[i]);
            }
        }

        void ILocalisationService.UnloadLocalisationData(string sheetName)
        {
            if (m_LoadedSheets.Remove(sheetName, out LocalisationData localisationData))
            {
                localisationData.Dispose();
            }
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
            foreach (LocalisationData localisationData in m_LoadedSheets.Values)
            {
                localisationData.Dispose();
            }

            m_LoadedSheets.Clear();
        }

        Task<string[]> ILocalisationService.GetAllLanguages()
        {
            return m_LocalisationSheetSourceProvider.GetAllLanguages();
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