namespace RPGFramework.Localisation.Editor
{
    internal interface ILocBinWriter
    {
        void GenerateLocBin(LocalisationMaster master, LocalisationSheetAsset asset);
        void GenerateLocMan(LocalisationMaster master);
    }
}