using System;
using System.IO;
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
    }

    internal interface ILocalisationSheetSourceProvider
    {
        Task<ILocalisationSheetSource> GetLocalisationSheetSource(string language);
        Task<string[]>                 GetAllLanguages();
    }

    internal interface ILocalisationSheetSource
    {
        Task<LocalisationData> LoadSheetAsync(string language, string sheetName);
    }

    internal interface IStreamingAssetLoader
    {
        Task<byte[]> LoadAsync(string path);
    }

    internal interface ILocalisationBinLoader
    {
        LocalisationData LoadLocBin(BinaryReader reader);
        string[]         LoadLocMan(BinaryReader reader);
    }
}