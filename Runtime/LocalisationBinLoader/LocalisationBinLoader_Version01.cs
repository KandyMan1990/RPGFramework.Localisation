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

        internal LocalisationBinLoader_Version01()
        {
            m_LocalisationBinLoader = this;
        }

        async Task<LocalisationData> ILocalisationBinLoader.LoadSheetAsync(string language, string sheetName)
        {
            string neutral = HelperFunctions.GetNeutralLanguage(language);

            byte[] bytes = await LocalisationBinFileLoader.LoadFileAsync(language, neutral, sheetName, VERSION);

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            LocalisationBinReader.ValidateHeader(reader, language, neutral, VERSION);

            return LocalisationBinReader.ReadLocalisationData(reader, (int)reader.BaseStream.Position, (int)reader.BaseStream.Length);
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