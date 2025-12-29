using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal sealed class LocalisationBinLoader_Version02 : ILocalisationBinLoader
    {
        private const byte VERSION = 2;

        private readonly ILocalisationBinLoader m_LocalisationBinLoader;

        private Dictionary<ulong, SheetData> m_TableOfContents;
        private string                       m_LoadedLanguage;

        internal LocalisationBinLoader_Version02()
        {
            m_TableOfContents       = new Dictionary<ulong, SheetData>();
            m_LocalisationBinLoader = this;
        }

        async Task<LocalisationData> ILocalisationBinLoader.LoadSheetAsync(string language, string sheetName)
        {
            LocalisationData[] data = await m_LocalisationBinLoader.LoadSheetsAsync(language, new[] { sheetName });

            return data[0];
        }

        async Task<LocalisationData[]> ILocalisationBinLoader.LoadSheetsAsync(string language, string[] sheetNames)
        {
            string neutral = HelperFunctions.GetNeutralLanguage(language);

            byte[] bytes = await LocalisationBinFileLoader.LoadFileAsync(language, neutral, null, VERSION);

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            if (m_LoadedLanguage != language)
            {
                m_LoadedLanguage = language;
                m_TableOfContents.Clear();
                m_TableOfContents = null;

                LocalisationBinReader.ValidateHeader(reader, language, neutral, VERSION);
            }

            LocalisationData[] data = ReadData(reader, sheetNames);

            return data;
        }

        private LocalisationData[] ReadData(BinaryReader binaryReader, string[] sheetNames)
        {
            ulong[] sheetHashes = new ulong[sheetNames.Length];
            for (int i = 0; i < sheetHashes.Length; i++)
            {
                sheetHashes[i] = Fnv1a64.Hash(sheetNames[i]);
            }

            if (m_TableOfContents == null)
            {
                uint sheetCount = binaryReader.ReadUInt32();
                m_TableOfContents = new Dictionary<ulong, SheetData>((int)sheetCount);

                for (int i = 0; i < sheetCount; i++)
                {
                    ulong sheetHash          = binaryReader.ReadUInt64();
                    uint  sheetStartPosition = binaryReader.ReadUInt32();
                    uint  sheetLength        = binaryReader.ReadUInt32();

                    m_TableOfContents.Add(sheetHash, new SheetData(sheetStartPosition, sheetLength));
                }
            }

            LocalisationData[] data = new LocalisationData[sheetHashes.Length];

            for (int i = 0; i < sheetHashes.Length; i++)
            {
                if (!m_TableOfContents.TryGetValue(sheetHashes[i], out SheetData sheetData))
                {
                    throw new KeyNotFoundException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadData)} Hash key not found for sheet [{sheetNames[i]}]");
                }

                data[i] = LocalisationBinReader.ReadLocalisationData(binaryReader, (int)sheetData.StartPosition, (int)sheetData.Length);
            }

            return data;
        }

        private readonly struct SheetData
        {
            internal readonly uint StartPosition;
            internal readonly uint Length;

            public SheetData(uint sheetStartPosition, uint sheetLength)
            {
                StartPosition = sheetStartPosition;
                Length        = sheetLength;
            }
        }
    }
}