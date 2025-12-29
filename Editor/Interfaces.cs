namespace RPGFramework.Localisation.Editor
{
    internal interface ILocalisationBinWriter
    {
        void GenerateLocalisationBin(LocalisationMaster      master, LocalisationSheetAsset asset);
        void GenerateLocalisationManifest(LocalisationMaster master);
    }
}