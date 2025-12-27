using System.Data;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class LocalisationBinWriterProvider
    {
        internal static ILocalisationBinWriter GetLocBinWriter(byte version)
        {
            return version switch
                   {
                           1 => new LocalisationBinWriter_Version01(),
                           _ => throw new VersionNotFoundException($"{nameof(LocalisationBinWriterProvider)}::{nameof(GetLocBinWriter)} version [{version}] is not registered")
                   };
        }
    }
}