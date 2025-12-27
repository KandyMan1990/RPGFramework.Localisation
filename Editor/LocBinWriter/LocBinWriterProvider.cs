using System.Data;

namespace RPGFramework.Localisation.Editor.LocBinWriter
{
    internal static class LocBinWriterProvider
    {
        internal static ILocBinWriter GetLocBinWriter(byte version)
        {
            return version switch
                   {
                           1 => new LocBinWriter_Version01(),
                           _ => throw new VersionNotFoundException($"LocBinWriter version [{version}] is not registered")
                   };
        }
    }
}