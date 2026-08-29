using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal sealed class LocalisationBinLoader_Version02 : ILocalisationBinLoader
    {
        private const byte VERSION = 2;

        private const int TOC_ENTRY_SIZE = sizeof(ulong) + sizeof(uint) + sizeof(uint);

        private readonly ILocalisationBinLoader m_LocalisationBinLoader;

        private Dictionary<ulong, SheetData> m_TableOfContents;
        private string                       m_LoadedLanguage;

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
            string neutral = HelperFunctions.GetNeutralLanguage(language);

            byte[] bytes = await LocalisationBinFileLoader.LoadFileAsync(language, neutral, null, VERSION);

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            Dictionary<ulong, SheetData> tableOfContents = m_TableOfContents;

            if (m_LoadedLanguage != language || tableOfContents == null)
            {
                LocalisationBinReader.ValidateHeader(reader, language, neutral, VERSION);

                tableOfContents = ReadTableOfContents(reader);

                m_TableOfContents = tableOfContents;
                m_LoadedLanguage  = language;
            }

            LocalisationData[] data = ReadData(reader, tableOfContents, sheetNames);

            return data;
        }

        private static Dictionary<ulong, SheetData> ReadTableOfContents(BinaryReader binaryReader)
        {
            Stream stream     = binaryReader.BaseStream;
            uint   sheetCount = binaryReader.ReadUInt32();
            long   maxSheets  = (stream.Length - stream.Position) / TOC_ENTRY_SIZE;

            if (sheetCount > maxSheets)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContents)} Sheet count [{sheetCount}] exceeds the [{maxSheets}] the remaining {stream.Length - stream.Position} bytes can hold");
            }

            Dictionary<ulong, SheetData> tableOfContents = new Dictionary<ulong, SheetData>((int)sheetCount);

            for (int i = 0; i < sheetCount; i++)
            {
                ulong sheetHash          = binaryReader.ReadUInt64();
                uint  sheetStartPosition = binaryReader.ReadUInt32();
                uint  sheetLength        = binaryReader.ReadUInt32();

                if (!tableOfContents.TryAdd(sheetHash, new SheetData(sheetStartPosition, sheetLength)))
                {
                    throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version02)}::{nameof(ReadTableOfContents)} Duplicate sheet hash [{sheetHash}] in the table of contents");
                }
            }

            return tableOfContents;
        }

        private static LocalisationData[] ReadData(BinaryReader binaryReader, Dictionary<ulong, SheetData> tableOfContents, string[] sheetNames)
        {
            LocalisationData[] data = new LocalisationData[sheetNames.Length];

            for (int i = 0; i < sheetNames.Length; i++)
            {
                ulong sheetHash = Fnv1a64.Hash(sheetNames[i]);

                if (!tableOfContents.TryGetValue(sheetHash, out SheetData sheetData))
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

            internal SheetData(uint sheetStartPosition, uint sheetLength)
            {
                StartPosition = sheetStartPosition;
                Length        = sheetLength;
            }
        }
    }
}