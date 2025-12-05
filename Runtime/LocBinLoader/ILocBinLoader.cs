using System.IO;

namespace RPGFramework.Localisation.LocBinLoader
{
    internal interface ILocBinLoader
    {
        LocData Load(BinaryReader binaryReader);
    }
}