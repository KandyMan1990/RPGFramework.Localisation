using System;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation
{
    public interface ILocalisationArgs
    {
        string[] DataSheetsToLoad { get; }
    }

    public interface ILocalisationService
    {
        event Action<string> OnLanguageChanged;
        string               CurrentLanguage { get; }
        Task                 SetCurrentLanguage(string language);
        Task<string[]>       GetAllLanguages();
        Task                 LoadNewLocalisationDataAsync(string   sheetName);
        Task                 LoadNewLocalisationDataAsync(string[] sheetNames);
        void                 UnloadLocalisationData(string         sheetName);
        void                 UnloadLocalisationData(string[]       sheetNames);
        void                 UnloadAllLocalisationData();
        string               Get(string key);
        string               Get(ulong  key);
    }

    internal interface IStreamingAssetLoader
    {
        Task<byte[]> LoadAsync(string path);
    }

    internal interface ILocalisationBinLoader
    {
        Task<LocalisationData>   LoadSheetAsync(string  language, string   sheetName);
        Task<LocalisationData[]> LoadSheetsAsync(string language, string[] sheetNames);
    }
}