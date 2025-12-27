namespace RPGFramework.Localisation.Editor
{
    internal interface ILocalisationBinWriter
    {
        void GenerateLocBin(LocalisationMaster master, LocalisationSheetAsset asset);
        void GenerateLocMan(LocalisationMaster master);
    }
}