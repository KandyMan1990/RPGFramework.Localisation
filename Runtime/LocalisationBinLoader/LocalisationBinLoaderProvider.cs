using System.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinLoaderProvider
    {
        internal static ILocalisationBinLoader Get(byte version)
        {
            return version switch
                   {
                           1 => new LocalisationBinLoader_Version01(),
                           2 => new LocalisationBinLoader_Version02(),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinLoaderProvider)}::{nameof(Get)} Version [{version}] not registered")
                   };
        }
    }
}