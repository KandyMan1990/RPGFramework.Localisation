using System.Data;
using RPGFramework.Localisation.LocBinLoader;

namespace RPGFramework.Localisation
{
    internal static class LocBinLoaderProvider
    {
        internal static ILocBinLoader GetLocBinLoader(byte version)
        {
            return version switch
                   {
                           1 => new LocBinLoader_Version01(),
                           _ => throw new VersionNotFoundException($"LocBinLoader version [{version}] is not registered")
                   };
        }
    }
}