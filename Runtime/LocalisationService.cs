using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RPGFramework.Localisation
{
    public interface ILocalisationService
    {
        event Action<string> OnLanguageChanged;
        string               CurrentLanguage { get; }
        void                 SetCurrentLanguage(string      language);
        void                 LoadNewLocalisationData(string sheetName);
        void                 ClearLocalisationData();
        string               Get(string                         key);
        void                 SetStreamingAssetsSubFolder(string folderName);
        string[]             GetAllLanguages();
    }

    public class LocalisationService : ILocalisationService
    {
        event Action<string> ILocalisationService.OnLanguageChanged
        {
            add => m_OnLanguageChanged += value;
            remove => m_OnLanguageChanged -= value;
        }

        string ILocalisationService.CurrentLanguage => m_CurrentLanguage;

        void ILocalisationService.SetCurrentLanguage(string language)
        {
            if (language == m_CurrentLanguage)
            {
                return;
            }

            m_CurrentLanguage = language;

            List<string> sheetNames = m_Data.Keys.ToList();

            UnloadAllSheets(m_Data);

            foreach (string sheetName in sheetNames)
            {
                m_LocalisationService.LoadNewLocalisationData(sheetName);
            }

            m_OnLanguageChanged?.Invoke(m_CurrentLanguage);
        }

        void ILocalisationService.LoadNewLocalisationData(string sheetName)
        {
            try
            {
                string filePath = Path.Combine(Application.streamingAssetsPath, m_SubFolder, m_LocalisationService.CurrentLanguage, $"{sheetName}.locbin");

                if (!File.Exists(filePath))
                {
                    string neutral = GetNeutralFrom(m_LocalisationService.CurrentLanguage);

                    if (!string.Equals(neutral, m_LocalisationService.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        string neutralPath = Path.Combine(Application.streamingAssetsPath, m_SubFolder, neutral, $"{sheetName}.locbin");

                        if (File.Exists(neutralPath))
                        {
                            filePath = neutralPath;
                        }
                    }

                    if (!File.Exists(filePath))
                    {
                        Debug.LogWarning($"{nameof(ILocalisationService)}::{nameof(GetSheetData)} No locbin for sheet <{sheetName}> lang <{m_LocalisationService.CurrentLanguage}>");
                        return;
                    }
                }

                byte[]  bytes   = File.ReadAllBytes(filePath);
                LocData locData = LocData.FromBytes(bytes);

                m_Data[sheetName] = locData;
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(ILocalisationService)}::{nameof(GetSheetData)} Failed to load locbin: {e}");
            }
        }

        void ILocalisationService.ClearLocalisationData()
        {
            UnloadAllSheets(m_Data);
        }

        string ILocalisationService.Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return $"MISSING KEY <{key}>";
            }

            string sheetName = key.Split('/')[0];
            string keyValue  = key.Split('/')[1];

            LocData data = GetSheetData(m_Data, sheetName, m_CurrentLanguage);

            if (data == null)
            {
                return $"MISSING SHEET <{sheetName}>";
            }

            ulong hash  = Fnv1a64.Hash(keyValue);
            int   index = BinarySearch(data.Hashes, hash);

            if (index < 0)
            {
                return $"MISSING KEY <{key}> IN SHEET <{sheetName}>";
            }

            int    offset = data.Offsets[index];
            string str    = ReadLengthPrefixedString(data.StringTable, offset);

            if (str == null)
            {
                return $"MISSING VALUE FOR <{key}> IN SHEET <{sheetName}>";
            }

            return str;
        }

        void ILocalisationService.SetStreamingAssetsSubFolder(string folderName)
        {
            m_SubFolder = folderName;
        }

        string[] ILocalisationService.GetAllLanguages()
        {
            string languagesPath = Path.Combine(Application.streamingAssetsPath, m_SubFolder);

            DirectoryInfo   dirInfo     = new DirectoryInfo(languagesPath);
            DirectoryInfo[] directories = dirInfo.GetDirectories();

            string[] languages = new string[directories.Length];

            for (int i = 0; i < directories.Length; i++)
            {
                languages[i] = directories[i].Name;
            }

            return languages;
        }

        private event Action<string> m_OnLanguageChanged;

        private readonly Dictionary<string, LocData> m_Data;
        private readonly ILocalisationService        m_LocalisationService;

        private string m_CurrentLanguage;
        private string m_SubFolder;

        public LocalisationService()
        {
            m_Data                = new Dictionary<string, LocData>();
            m_LocalisationService = this;
            m_CurrentLanguage     = "en-GB";
            m_SubFolder           = "Localisation";
        }

        private static void UnloadAllSheets(Dictionary<string, LocData> data)
        {
            foreach (KeyValuePair<string, LocData> kvp in data)
            {
                kvp.Value.Dispose();
            }

            data.Clear();
        }

        private static LocData GetSheetData(Dictionary<string, LocData> data, string sheetName, string currentLanguage)
        {
            if (data.TryGetValue(sheetName, out LocData existing))
            {
                return existing;
            }

            Debug.LogWarning($"{nameof(ILocalisationService)}::{nameof(GetSheetData)} [{sheetName}] [{currentLanguage}] not set");
            return null;
        }

        private static string GetNeutralFrom(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return language;
            }

            string[] parts = language.Split('-', '_');

            return parts.Length > 0 ? parts[0] : language;
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

        private static string ReadLengthPrefixedString(byte[] table, int offset)
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

        private class LocData : IDisposable
        {
            public ulong[] Hashes      { get; private set; }
            public int[]   Offsets     { get; private set; }
            public byte[]  StringTable { get; private set; }

            private LocData(ulong[] hashes, int[] offsets, byte[] stringTable)
            {
                Hashes      = hashes;
                Offsets     = offsets;
                StringTable = stringTable;
            }

            public static LocData FromBytes(byte[] bytes)
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte[] magic = br.ReadBytes(4);

                    if (magic[0] != (byte)'L' || magic[1] != (byte)'O' || magic[2] != (byte)'C' || magic[3] != (byte)'B')
                    {
                        throw new InvalidDataException("Invalid locbin magic");
                    }

                    int     version = br.ReadInt32();
                    int     count   = br.ReadInt32();
                    ulong[] hashes  = new ulong[count];
                    int[]   offsets = new int[count];

                    for (int i = 0; i < count; i++)
                    {
                        hashes[i]  = br.ReadUInt64();
                        offsets[i] = br.ReadInt32();
                    }

                    int    remaining = (int)(ms.Length - ms.Position);
                    byte[] table     = br.ReadBytes(remaining);

                    return new LocData(hashes, offsets, table);
                }
            }

            public void Dispose()
            {
                Hashes      = null;
                Offsets     = null;
                StringTable = null;
            }
        }
    }
}