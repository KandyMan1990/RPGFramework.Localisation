using System.Data;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class LocalisationBinGeneratorProvider
    {
        internal static ILocalisationBinGenerator GetLocalisationBinGenerator(byte version)
        {
            return version switch
                   {
                           1 => new LocalisationBinGenerator_Version01(),
                           2 => new LocalisationBinGenerator_Version02(),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinGeneratorProvider)}::{nameof(GetLocalisationBinGenerator)} version [{version}] is not registered")
                   };
        }
    }
}