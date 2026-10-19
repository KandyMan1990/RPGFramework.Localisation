using System.IO;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;
using RPGFramework.Localisation.Helpers;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal sealed class LocalisationBinLoader_Version01 : ILocalisationBinLoader
    {
        private const byte VERSION = 1;

        private readonly ILocalisationBinLoader m_LocalisationBinLoader;
        private readonly IStreamingAssetLoader  m_AssetLoader;

        internal LocalisationBinLoader_Version01(IStreamingAssetLoader assetLoader)
        {
            m_LocalisationBinLoader = this;
            m_AssetLoader           = assetLoader;
        }

        async Task<LocalisationData> ILocalisationBinLoader.LoadSheetAsync(string language, string sheetName)
        {
            string neutral = HelperFunctions.GetNeutralLanguage(language);

            byte[] bytes = await LocalisationBinFileLoader.LoadFileAsync(m_AssetLoader, language, neutral, sheetName, VERSION);

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            LocalisationBinReader.ValidateHeader(reader, language, neutral, VERSION);

            int startPosition = (int)stream.Position;
            int payloadLength = (int)(stream.Length - startPosition);

            return LocalisationBinReader.ReadLocalisationData(reader, startPosition, payloadLength);
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
    }
}