namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    /// <summary>
    /// One generated file, built in memory and not yet written to disk.
    /// </summary>
    internal readonly struct LocalisationBinFile
    {
        internal readonly string Path;
        internal readonly byte[] Bytes;

        internal LocalisationBinFile(string path, byte[] bytes)
        {
            Path  = path;
            Bytes = bytes;
        }
    }
}