using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    /// <summary>
    /// One .locbin per language, holding every sheet.
    /// </summary>
    /// <remarks>
    /// The header and table of contents are read once per language and cached; each sheet is then read by
    /// seeking to its recorded offset and reading its recorded length. Only the sheets actually asked for are
    /// read, so loading one sheet does not pull in the rest of the language.
    /// </remarks>
    internal sealed class LocalisationBinLoader_Version02 : ILocalisationBinLoader
    {
        private const byte VERSION = 2;

        private const int TOC_ENTRY_SIZE     = sizeof(ulong) + sizeof(uint) + sizeof(uint);
        private const int HEADER_PROBE_BYTES = 1024;

        private readonly ILocalisationBinLoader m_LocalisationBinLoader;

        private Dictionary<ulong, SheetData> m_TableOfContents;
        private string                       m_LoadedLanguage;
        private string                       m_LoadedPath;

        internal LocalisationBinLoader_Version02()
        {
            m_LocalisationBinLoader = this;
        }

        async Task<LocalisationData> ILocalisationBinLoader.LoadSheetAsync(string language, string sheetName)
        {
            LocalisationData[] data = await m_LocalisationBinLoader.LoadSheetsAsync(language, new[] { sheetName });

            return data[0];
        }

        async Task<LocalisationData[]> ILocalisationBinLoader.LoadSheetsAsync(string language, string[] sheetNames)
        {
            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            if (m_LoadedLanguage != language || m_TableOfContents == null)
            {
                string neutral = HelperFunctions.GetNeutralLanguage(language);
                string path    = await LocalisationBinFileLoader.ResolvePathAsync(language, neutral, null, VERSION);

                Dictionary<ulong, SheetData> tableOfContents = await ReadTableOfContentsAsync(assetLoader, path, language, neutral);

                m_TableOfContents = tableOfContents;
                m_LoadedLanguage  = language;
                m_LoadedPath      = path;
            }

            LocalisationData[] data = await ReadDataAsync(assetLoader, m_LoadedPath, m_TableOfContents, sheetNames);

            return data;
        }

        private static async Task<Dictionary<ulong, SheetData>> ReadTableOfContentsAsync(IStreamingAssetLoader assetLoader, string path, string language, string neutralLanguage)
        {
            byte[] prefix = await assetLoader.LoadRangeAsync(path, 0, HEADER_PROBE_BYTES);

            if (prefix == null || prefix.Length == 0)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContentsAsync)} [{path}] is empty");
            }

            uint sheetCount;
            long tableStart;

            using (MemoryStream headerStream = new MemoryStream(prefix))
            using (BinaryReader headerReader = new BinaryReader(headerStream))
            {
                LocalisationBinReader.ValidateHeader(headerReader, language, neutralLanguage, VERSION);

                sheetCount = headerReader.ReadUInt32();
                tableStart = headerStream.Position;
            }

            long tableSize = (long)sheetCount * TOC_ENTRY_SIZE;

            if (tableSize > int.MaxValue)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContentsAsync)} Sheet count [{sheetCount}] in [{path}] is not credible");
            }

            byte[] tableBytes = await assetLoader.LoadRangeAsync(path, tableStart, (int)tableSize);

            if (tableBytes == null || tableBytes.Length != tableSize)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContentsAsync)} Sheet count [{sheetCount}] exceeds what [{path}] can hold");
            }

            Dictionary<ulong, SheetData> tableOfContents = new Dictionary<ulong, SheetData>((int)sheetCount);

            using MemoryStream tableStream = new MemoryStream(tableBytes);
            using BinaryReader tableReader = new BinaryReader(tableStream);

            for (int i = 0; i < sheetCount; i++)
            {
                ulong sheetHash          = tableReader.ReadUInt64();
                uint  sheetStartPosition = tableReader.ReadUInt32();
                uint  sheetLength        = tableReader.ReadUInt32();

                if (!tableOfContents.TryAdd(sheetHash, new SheetData(sheetStartPosition, sheetLength)))
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContentsAsync)} Duplicate sheet hash [{sheetHash}] in the table of contents");
                }
            }

            return tableOfContents;
        }

        private static async Task<LocalisationData[]> ReadDataAsync(IStreamingAssetLoader assetLoader, string path, Dictionary<ulong, SheetData> tableOfContents, string[] sheetNames)
        {
            LocalisationData[] data = new LocalisationData[sheetNames.Length];

            for (int i = 0; i < sheetNames.Length; i++)
            {
                ulong sheetHash = Fnv1a64.Hash(sheetNames[i]);

                if (!tableOfContents.TryGetValue(sheetHash, out SheetData sheetData))
                {
                    throw new KeyNotFoundException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadDataAsync)} Hash key not found for sheet [{sheetNames[i]}]");
                }

                byte[] payload = await assetLoader.LoadRangeAsync(path, sheetData.StartPosition, (int)sheetData.Length);

                if (payload == null || payload.Length != sheetData.Length)
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadDataAsync)} Sheet [{sheetNames[i]}] runs past the end of [{path}]");
                }

                using MemoryStream stream = new MemoryStream(payload);
                using BinaryReader reader = new BinaryReader(stream);

                data[i] = LocalisationBinReader.ReadLocalisationData(reader, 0, payload.Length);
            }

            return data;
        }

        private readonly struct SheetData
        {
            internal readonly uint StartPosition;
            internal readonly uint Length;

            internal SheetData(uint sheetStartPosition, uint sheetLength)
            {
                StartPosition = sheetStartPosition;
                Length        = sheetLength;
            }
        }
    }
}