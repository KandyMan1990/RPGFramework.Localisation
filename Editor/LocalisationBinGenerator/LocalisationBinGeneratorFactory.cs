using System.Data;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class LocalisationBinGeneratorFactory
    {
        internal static ILocalisationBinGenerator Create(byte version)
        {
            ILocalisationBinGenerator generator = version switch
                                                  {
                                                      1 => new LocalisationBinGenerator_Version01(),
                                                      2 => new LocalisationBinGenerator_Version02(),
                                                      _ => throw new VersionNotFoundException($"{nameof(LocalisationBinGeneratorFactory)}::{nameof(Create)} version [{version}] is not registered")
                                                  };

            return generator;
        }
    }
}