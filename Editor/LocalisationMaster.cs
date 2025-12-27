using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    public enum LocalisationVersion
    {
        FilePerSheet    = 1,
        FilePerLanguage = 2
    }

    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Master", fileName = "LocalisationMaster")]
    public class LocalisationMaster : ScriptableObject
    {
        [Tooltip("GoogleSheet ID")]
        public string SheetId;

        [Tooltip("The default namespace to use in each sheet, can be overriden in the sheet Scriptable Object")]
        public string DefaultNamespace = "GameName.Localisation";

        [Tooltip("Which version of the .locbin format to use")]
        public LocalisationVersion Version;

        [Tooltip("LocalisationSheetAsset files to generate .locbin and manifest files")]
        public LocalisationSheetAsset[] SheetAssets;
    }
}