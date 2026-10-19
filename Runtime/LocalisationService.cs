using System;
using System.Threading.Tasks;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// Runtime localisation lookup, from the files in StreamingAssets.
    /// </summary>
    /// <remarks>
    /// A key identifies both its sheet and its entry: the hash stored in a locbin is
    /// <c>Fnv1a64.Hash("SheetName/EntryKey")</c>, so keys are unique across sheets and a lookup is a single
    /// dictionary hit. Generated key constants are already in that form, which is why <see cref="ILocalisationService.Get(string)"/>
    /// hashes the whole string rather than splitting it.
    /// </remarks>
    public sealed class LocalisationService : ILocalisationService
    {
        private readonly ILocalisationService m_Localisation;

        public LocalisationService()
        {
            m_Localisation = new StreamingAssetLocalisation(StreamingAssetLoaderProvider.Get());
        }

        event Action<string> ILocalisationService.OnLanguageChanged
        {
            add => m_Localisation.OnLanguageChanged += value;
            remove => m_Localisation.OnLanguageChanged -= value;
        }

        string ILocalisationService.CurrentLanguage => m_Localisation.CurrentLanguage;

        Task ILocalisationService.InitialiseAsync() => m_Localisation.InitialiseAsync();

        Task ILocalisationService.SetCurrentLanguage(string language) => m_Localisation.SetCurrentLanguage(language);

        Task<string[]> ILocalisationService.GetAllLanguages() => m_Localisation.GetAllLanguages();

        Task ILocalisationService.LoadNewLocalisationDataAsync(string sheetName) => m_Localisation.LoadNewLocalisationDataAsync(sheetName);

        Task ILocalisationService.LoadNewLocalisationDataAsync(string[] sheetNames) => m_Localisation.LoadNewLocalisationDataAsync(sheetNames);

        void ILocalisationService.UnloadLocalisationData(string sheetName) => m_Localisation.UnloadLocalisationData(sheetName);

        void ILocalisationService.UnloadLocalisationData(string[] sheetNames) => m_Localisation.UnloadLocalisationData(sheetNames);

        void ILocalisationService.UnloadAllLocalisationData() => m_Localisation.UnloadAllLocalisationData();

        string ILocalisationService.Get(string key) => m_Localisation.Get(key);

        string ILocalisationService.Get(ulong key) => m_Localisation.Get(key);

        bool ILocalisationService.TryGet(string key, out string value) => m_Localisation.TryGet(key, out value);

        bool ILocalisationService.TryGet(ulong key, out string value) => m_Localisation.TryGet(key, out value);
    }
}
