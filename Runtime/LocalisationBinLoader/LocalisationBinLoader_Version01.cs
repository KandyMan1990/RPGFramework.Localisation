using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal class LocalisationBinLoader_Version01 : ILocalisationBinLoader
    {
        private readonly ILocalisationBinLoader m_LocalisationBinLoader;

        public LocalisationBinLoader_Version01()
        {
            m_LocalisationBinLoader = this;
        }

        async Task<LocalisationData> ILocalisationBinLoader.LoadSheetAsync(string language, string sheetName)
        {
            string neutral = HelperFunctions.GetNeutralLanguage(language);

            string primary  = HelperFunctions.CombinePath(Constants.BasePath, language, $"{sheetName}.locbin");
            string fallback = HelperFunctions.CombinePath(Constants.BasePath, neutral,  $"{sheetName}.locbin");

            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            byte[] bytes = await assetLoader.LoadAsync(primary) ?? await assetLoader.LoadAsync(fallback);

            if (bytes == null)
            {
                throw new FileNotFoundException($"{nameof(ILocalisationBinLoader)}::{nameof(ILocalisationBinLoader.LoadSheetAsync)} Missing .locbin for sheet [{sheetName}] (language=[{language}], neutral=[{neutral}]");
            }

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            ValidateHeader(reader, language, neutral);

            return ReadData(reader);
        }

        Task<LocalisationData[]> ILocalisationBinLoader.LoadSheetsAsync(string language, string[] sheetNames)
        {
            Task<LocalisationData>[] data = new Task<LocalisationData>[sheetNames.Length];

            for (int i = 0; i < sheetNames.Length; i++)
            {
                data[i] = m_LocalisationBinLoader.LoadSheetAsync(language, sheetNames[i]);
            }

            return Task.WhenAll(data);
        }

        private static void ValidateHeader(BinaryReader reader, string language, string neutralLanguage)
        {
            byte[] magic = reader.ReadBytes(Constants.LocBinMagic.Length);
            if (!magic.SequenceEqual(Constants.LocBinMagic))
            {
                throw new InvalidDataException("Invalid locbin magic");
            }

            byte version = reader.ReadByte();
            if (version != 1)
            {
                throw new VersionNotFoundException($"{nameof(LocalisationBinLoader_Version01)}::{nameof(ValidateHeader)} Expected version [1] but locbin version is [{version}]");
            }

            byte   cultureLength = reader.ReadByte();
            string fileCulture   = Encoding.UTF8.GetString(reader.ReadBytes(cultureLength));

            if (fileCulture != language && fileCulture != neutralLanguage)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinLoader_Version01)}::{nameof(ValidateHeader)} Invalid locbin language, expected [{language}] or [{neutralLanguage}] but file is [{fileCulture}]");
            }
        }

        private static LocalisationData ReadData(BinaryReader binaryReader)
        {
            int count = binaryReader.ReadInt32();

            ulong[] hashes  = new ulong[count];
            int[]   offsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]  = binaryReader.ReadUInt64();
                offsets[i] = binaryReader.ReadInt32();
            }

            int    remaining = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);
            byte[] table     = binaryReader.ReadBytes(remaining);

            return new LocalisationData(hashes, offsets, table);
        }
    }
}