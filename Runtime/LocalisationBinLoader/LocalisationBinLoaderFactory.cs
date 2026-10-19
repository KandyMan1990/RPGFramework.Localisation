using System.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinLoaderFactory
    {
        internal static ILocalisationBinLoader Create(byte version, IStreamingAssetLoader assetLoader)
        {
            ILocalisationBinLoader loader = version switch
                                            {
                                                1 => new LocalisationBinLoader_Version01(assetLoader),
                                                2 => new LocalisationBinLoader_Version02(assetLoader),
                                                _ => throw new VersionNotFoundException($"{nameof(LocalisationBinLoaderFactory)}::{nameof(Create)} Version [{version}] not registered")
                                            };

            return loader;
        }
    }
}