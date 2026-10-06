using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    internal enum LocalisationVersion
    {
        FilePerSheet    = 1,
        FilePerLanguage = 2
    }

    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Master", fileName = "LocalisationMaster")]
    internal class LocalisationMaster : ScriptableObject
    {
        [Tooltip("The Google spreadsheet's id, or a link to it. It must be shared as \"anyone with the link can view\"")]
        public string SheetId;

        [Tooltip("The default namespace to use in each sheet, can be overriden in the sheet Scriptable Object")]
        public string DefaultNamespace = "GameName.Localisation";

        [Tooltip("The folder each sheet's generated keys class, a C# script, goes in unless the sheet overrides it. The folder decides which assembly compiles the class")]
        public string DefaultKeysClassFolder = "Assets/Scripts/Localisation";

        [Tooltip("Which version of the .locbin format to use")]
        public LocalisationVersion Version = LocalisationVersion.FilePerSheet;

        [Tooltip("A sheet asset per tab, in the spreadsheet's order, kept by Pull in the Sheets folder beside this asset")]
        public LocalisationSheetAsset[] SheetAssets;
    }
}