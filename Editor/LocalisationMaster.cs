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
        [Tooltip("GoogleSheet ID")]
        public string SheetId;

        [Tooltip("The default namespace to use in each sheet, can be overriden in the sheet Scriptable Object")]
        public string DefaultNamespace = "GameName.Localisation";

        [Tooltip("Which version of the .locbin format to use")]
        public LocalisationVersion Version = LocalisationVersion.FilePerSheet;

        [Tooltip("LocalisationSheetAsset files to generate .locbin and manifest files")]
        public LocalisationSheetAsset[] SheetAssets;
    }
}