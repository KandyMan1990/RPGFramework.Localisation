namespace RPGFramework.Localisation.Manifest
{
    internal sealed class ManifestData
    {
        internal byte     Version   { get; }
        internal string[] Languages { get; }

        public ManifestData(byte version, string[] languages)
        {
            Version   = version;
            Languages = languages;
        }
    }
}