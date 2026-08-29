using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.LocalisationBinLoader;
using RPGFramework.Localisation.Manifest;
using UnityEngine;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// Runtime localisation lookup.
    /// </summary>
    /// <remarks>
    /// A key identifies both its sheet and its entry: the hash stored in a locbin is
    /// <c>Fnv1a64.Hash("SheetName/EntryKey")</c>, so keys are unique across sheets and a lookup is a single
    /// dictionary hit. Generated key constants are already in that form, which is why <see cref="ILocalisationService.Get(string)"/>
    /// hashes the whole string rather than splitting it.
    /// </remarks>
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
        private readonly Dictionary<ulong, StringRef>         m_Index;
        private readonly SemaphoreSlim                        m_Gate;
        private readonly string                               m_RequestedLanguage;

        private string                 m_CurrentLanguage;
        private ManifestData           m_Manifest;
        private ILocalisationBinLoader m_LocalisationBinLoader;
        private Task                   m_ManifestTask;

        public LocalisationService()
        {
            m_RequestedLanguage = CultureInfo.CurrentUICulture.Name;
            m_CurrentLanguage   = m_RequestedLanguage;
            m_LoadedSheets      = new Dictionary<string, LocalisationData>();
            m_Index             = new Dictionary<ulong, StringRef>();
            m_Gate              = new SemaphoreSlim(1, 1);
            m_ManifestTask      = LoadManifestAsync();
        }

        /// <summary>
        /// Picks the best language the game actually ships for a requested one.
        /// </summary>
        /// <remarks>
        /// For a machine set to <c>en-US</c> where the game ships <c>en-GB</c> and <c>fr-FR</c>:
        /// <list type="number">
        /// <item>exact match — <c>en-US</c>, not present</item>
        /// <item>the neutral language — <c>en</c>, not present</item>
        /// <item>any pack sharing that neutral — <c>en-GB</c>, chosen</item>
        /// <item>otherwise the first language the game ships</item>
        /// </list>
        /// </remarks>
        private static string ResolveLanguage(string requested, string[] available)
        {
            if (!string.IsNullOrEmpty(requested))
            {
                for (int i = 0; i < available.Length; i++)
                {
                    if (string.Equals(available[i], requested, StringComparison.OrdinalIgnoreCase))
                    {
                        return available[i];
                    }
                }

                string neutral = HelperFunctions.GetNeutralLanguage(requested);

                for (int i = 0; i < available.Length; i++)
                {
                    if (string.Equals(available[i], neutral, StringComparison.OrdinalIgnoreCase))
                    {
                        return available[i];
                    }
                }

                for (int i = 0; i < available.Length; i++)
                {
                    string candidateNeutral = HelperFunctions.GetNeutralLanguage(available[i]);

                    if (string.Equals(candidateNeutral, neutral, StringComparison.OrdinalIgnoreCase))
                    {
                        return available[i];
                    }
                }
            }

            string fallback = available[0];

            return fallback;
        }

        async Task ILocalisationService.SetCurrentLanguage(string language)
        {
            if (language == m_CurrentLanguage)
            {
                return;
            }

            await EnsureManifestAsync();

            if (Array.IndexOf(m_Manifest.Languages, language) < 0)
            {
                throw new InvalidDataException($"{nameof(ILocalisationService)}::{nameof(ILocalisationService.SetCurrentLanguage)} Language [{language}] is not supported");
            }

            await m_Gate.WaitAsync();

            try
            {
                if (language == m_CurrentLanguage)
                {
                    return;
                }

                string[] sheetNames = new string[m_LoadedSheets.Count];
                m_LoadedSheets.Keys.CopyTo(sheetNames, 0);

                LocalisationData[] data = sheetNames.Length == 0
                                              ? Array.Empty<LocalisationData>()
                                              : await m_LocalisationBinLoader.LoadSheetsAsync(language, sheetNames);

                m_LoadedSheets.Clear();
                m_Index.Clear();

                for (int i = 0; i < sheetNames.Length; i++)
                {
                    m_LoadedSheets.Add(sheetNames[i], data[i]);
                    AddToIndex(sheetNames[i], data[i]);
                }

                m_CurrentLanguage = language;
            }
            finally
            {
                m_Gate.Release();
            }

            m_OnLanguageChanged?.Invoke(m_CurrentLanguage);
        }

        Task ILocalisationService.LoadNewLocalisationDataAsync(string sheetName)
        {
            return LoadSheetsAsync(new[] { sheetName });
        }

        Task ILocalisationService.LoadNewLocalisationDataAsync(string[] sheetNames)
        {
            return LoadSheetsAsync(sheetNames);
        }

        void ILocalisationService.UnloadLocalisationData(string sheetName)
        {
            RemoveSheet(sheetName);
        }

        void ILocalisationService.UnloadLocalisationData(string[] sheetNames)
        {
            for (int i = 0; i < sheetNames.Length; i++)
            {
                RemoveSheet(sheetNames[i]);
            }
        }

        void ILocalisationService.UnloadAllLocalisationData()
        {
            m_LoadedSheets.Clear();
            m_Index.Clear();
        }

        async Task<string[]> ILocalisationService.GetAllLanguages()
        {
            await EnsureManifestAsync();

            // A copy: the manifest's array is shared state and callers have no reason to be able to edit it.
            string[] languages = new string[m_Manifest.Languages.Length];
            Array.Copy(m_Manifest.Languages, languages, languages.Length);

            return languages;
        }

        string ILocalisationService.Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return $"MISSING KEY [{key}]";
            }

            if (TryGetString(Fnv1a64.Hash(key), out string value))
            {
                return value;
            }

            string miss = DescribeMiss(key);

            return miss;
        }

        string ILocalisationService.Get(ulong key)
        {
            if (TryGetString(key, out string value))
            {
                return value;
            }

            return $"MISSING KEY [{key}]";
        }

        private bool TryGetString(ulong key, out string value)
        {
            if (!m_Index.TryGetValue(key, out StringRef stringRef))
            {
                value = null;

                return false;
            }

            value = ReadString(stringRef.Table, stringRef.Offset);

            return value != null;
        }

        private string DescribeMiss(string key)
        {
            int slash = key.IndexOf('/');

            if (slash <= 0 || slash == key.Length - 1)
            {
                return $"MISSING KEY [{key}] (expected the form \"Sheet/Key\")";
            }

            string sheetName = key[..slash];

            if (!m_LoadedSheets.ContainsKey(sheetName))
            {
                return $"MISSING SHEET [{sheetName}]";
            }

            return $"MISSING KEY [{key}] IN SHEET [{sheetName}]";
        }

        private async Task LoadSheetsAsync(string[] sheetNames)
        {
            await EnsureManifestAsync();

            await m_Gate.WaitAsync();

            try
            {
                for (int i = 0; i < sheetNames.Length; i++)
                {
                    string sheetName = sheetNames[i];

                    if (m_LoadedSheets.ContainsKey(sheetName))
                    {
                        throw new InvalidOperationException($"{nameof(LocalisationService)}::{nameof(LoadSheetsAsync)} Sheet [{sheetName}] is already loaded. It must be unloaded before it is requested again");
                    }
                }

                LocalisationData[] data = await m_LocalisationBinLoader.LoadSheetsAsync(m_CurrentLanguage, sheetNames);

                for (int i = 0; i < sheetNames.Length; i++)
                {
                    m_LoadedSheets[sheetNames[i]] = data[i];
                    AddToIndex(sheetNames[i], data[i]);
                }
            }
            finally
            {
                m_Gate.Release();
            }
        }

        private void RemoveSheet(string sheetName)
        {
            bool removed = m_LoadedSheets.Remove(sheetName, out LocalisationData data);

            if (!removed)
            {
                return;
            }

            for (int i = 0; i < data.Hashes.Length; i++)
            {
                m_Index.Remove(data.Hashes[i]);
            }
        }

        private void AddToIndex(string sheetName, LocalisationData data)
        {
            for (int i = 0; i < data.Hashes.Length; i++)
            {
                StringRef stringRef = new StringRef(data.StringTable, data.Offsets[i]);

                if (m_Index.TryAdd(data.Hashes[i], stringRef))
                {
                    continue;
                }

                // Keys carry their sheet, so two sheets cannot produce the same hash for the same entry name.
                // Reaching here means a genuine FNV-1a collision between two different keys. Keep the first and
                // report it: the generator validates uniqueness within a sheet but cannot see across sheets
                // that are never loaded together.
                Debug.LogError($"{nameof(LocalisationService)}::{nameof(AddToIndex)} Hash [{data.Hashes[i]}] in sheet [{sheetName}] collides with an already-loaded key. The colliding entry will be unreachable; rename one of the keys.");
            }
        }

        private Task EnsureManifestAsync()
        {
            if (m_ManifestTask == null || m_ManifestTask.IsFaulted || m_ManifestTask.IsCanceled)
            {
                m_ManifestTask = LoadManifestAsync();
            }

            return m_ManifestTask;
        }

        private async Task LoadManifestAsync()
        {
            m_Manifest = await ManifestProvider.GetManifestAsync();

            m_CurrentLanguage = ResolveLanguage(m_RequestedLanguage, m_Manifest.Languages);

            m_LocalisationBinLoader = LocalisationBinLoaderProvider.Get(m_Manifest.Version);
        }

        private static string ReadString(byte[] table, int offset)
        {
            if (table == null)
            {
                return null;
            }

            if (offset < 0 || offset > table.Length - sizeof(int))
            {
                return null;
            }

            int length = BitConverter.ToInt32(table, offset);

            if (length < 0 || length > table.Length - offset - sizeof(int))
            {
                return null;
            }

            string str = Encoding.UTF8.GetString(table, offset + sizeof(int), length);

            return str;
        }

        /// <summary>
        /// Where one entry's text lives: the owning sheet's string table and the offset of its length prefix.
        /// </summary>
        private readonly struct StringRef
        {
            internal readonly byte[] Table;
            internal readonly int    Offset;

            internal StringRef(byte[] table, int offset)
            {
                Table  = table;
                Offset = offset;
            }
        }
    }
}