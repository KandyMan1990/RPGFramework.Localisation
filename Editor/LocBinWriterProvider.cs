using System.Data;
using RPGFramework.Localisation.Editor.LocBinWriter;

namespace RPGFramework.Localisation.Editor
{
    internal static class LocBinWriterProvider
    {
        internal static ILocBinWriter GetLocBinWriter(int version)
        {
            return version switch
                   {
                           1 => new LocBinWriter_Version01(),
                           _ => throw new VersionNotFoundException($"LocBinWriter version [{version}] is not registered")
                   };
        }
    }
}