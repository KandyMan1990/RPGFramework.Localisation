using System.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinLoaderProvider
    {
        internal static ILocalisationBinLoader GetLocalisationBinLoader(byte version)
        {
            return version switch
                   {
                           1 => new LocalisationBinLoader_Version01(),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinLoaderProvider)}::{nameof(GetLocalisationBinLoader)} version [{version}] is not registered")
                   };
        }
    }
}