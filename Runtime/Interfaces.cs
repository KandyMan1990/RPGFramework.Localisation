using System;
using System.Threading.Tasks;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation
{
    /// <summary>The sheets a screen needs.</summary>
    public interface ILocalisationArgs
    {
        /// <summary>The names of the sheets to load, as their tabs are named.</summary>
        string[] DataSheetsToLoad { get; }
    }

    /// <summary>
    /// Loads sheets of text from StreamingAssets in the current language and looks text up by key, written
    /// <c>Sheet/Key</c> or as that string's FNV-1a 64 hash.
    /// </summary>
    public interface ILocalisationService
    {
        /// <summary>Raised with the new language once every loaded sheet has been reloaded in it.</summary>
        event Action<string> OnLanguageChanged;

        /// <summary>
        /// The language text is read in: once initialised, the device's language or the closest the game ships.
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Reads the manifest and settles the starting language. Await it before any text is loaded or shown; it throws
        /// when the manifest cannot be read.
        /// </summary>
        Task InitialiseAsync();

        /// <summary>
        /// Switches to a language the game ships, reloading every loaded sheet in it, then raises
        /// <see cref="OnLanguageChanged" />. Throws for a language the game does not ship; the current one does nothing.
        /// </summary>
        Task SetCurrentLanguage(string language);

        /// <summary>Every language the game ships, as the manifest lists them.</summary>
        Task<string[]> GetAllLanguages();

        /// <summary>Loads a sheet in the current language. Throws if it is already loaded.</summary>
        Task LoadNewLocalisationDataAsync(string sheetName);

        /// <summary>Loads these sheets in the current language. Throws if any is already loaded.</summary>
        Task LoadNewLocalisationDataAsync(string[] sheetNames);

        /// <summary>Unloads a sheet; one not loaded is ignored.</summary>
        void UnloadLocalisationData(string sheetName);

        /// <summary>Unloads these sheets; any not loaded are ignored.</summary>
        void UnloadLocalisationData(string[] sheetNames);

        /// <summary>Unloads every sheet.</summary>
        void UnloadAllLocalisationData();

        /// <summary>
        /// The text for a <c>Sheet/Key</c> key, or a marker naming what is missing — <c>MISSING KEY [...]</c> or
        /// <c>MISSING SHEET [...]</c> — so a mistake shows on screen.
        /// </summary>
        string Get(string key);

        /// <summary>The text for a key's hash, or <c>MISSING KEY [...]</c> naming the hash.</summary>
        string Get(ulong key);

        /// <summary>Whether a <c>Sheet/Key</c> key is in a loaded sheet, and its text if so.</summary>
        bool TryGet(string key, out string value);

        /// <summary>Whether a key's hash is in a loaded sheet, and its text if so.</summary>
        bool TryGet(ulong key, out string value);
    }

    /// <summary>Reads files from StreamingAssets: from the file system, or by web request on Android and WebGL.</summary>
    internal interface IStreamingAssetLoader
    {
        /// <summary>The whole file, or null when there is none.</summary>
        Task<byte[]> LoadAsync(string path);

        /// <summary>Up to <paramref name="length" /> bytes from <paramref name="offset" />, or null when there is no file.</summary>
        Task<byte[]> LoadRangeAsync(string path, long offset, int length);
    }

    /// <summary>Reads sheets in a certain file format.</summary>
    internal interface ILocalisationBinLoader
    {
        /// <summary>One sheet in a language, falling back to its neutral language's file when the regional one is missing.</summary>
        Task<LocalisationData> LoadSheetAsync(string language, string sheetName);

        /// <summary>These sheets in a language, in the order asked for.</summary>
        Task<LocalisationData[]> LoadSheetsAsync(string language, string[] sheetNames);
    }
}