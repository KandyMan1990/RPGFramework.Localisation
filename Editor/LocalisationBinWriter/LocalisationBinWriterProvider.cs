using System.Data;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class LocalisationBinWriterProvider
    {
        internal static ILocalisationBinWriter GetLocalisationBinWriter(byte version)
        {
            return version switch
                   {
                           1 => new LocalisationBinWriter_Version01(),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinWriterProvider)}::{nameof(GetLocalisationBinWriter)} version [{version}] is not registered")
                   };
        }
    }
}