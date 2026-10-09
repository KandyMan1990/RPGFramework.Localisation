using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    internal enum LocalisationVersion
    {
        FilePerSheet    = 1,
        FilePerLanguage = 2
    }

    internal enum SpreadsheetAccess
    {
        SharedLink = 0,
        WebApp     = 1
    }

    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Master", fileName = "LocalisationMaster")]
    internal class LocalisationMaster : ScriptableObject
    {
        [Tooltip("How Pull reaches the spreadsheet: by its link, which anyone with the link can view, or through a web app deployed from it, which keeps it private")]
        public SpreadsheetAccess Access = SpreadsheetAccess.SharedLink;

        [Tooltip("The Google spreadsheet's id, or a link to it. It must be shared as \"anyone with the link can view\"")]
        public string SheetId;

        [Tooltip("The URL of the web app deployed from the spreadsheet's script. Anyone holding it can read the spreadsheet, so keep it as you would a password")]
        public string WebAppUrl;

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