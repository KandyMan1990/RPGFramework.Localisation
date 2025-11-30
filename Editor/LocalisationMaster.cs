using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Master", fileName = "LocalisationMaster")]
    public class LocalisationMaster : ScriptableObject
    {
        [Tooltip("GoogleSheet ID")]
        public string SheetId;

        [Tooltip("Base sub folder inside StreamingAssets where locbin files are written (defaults to Localisation)")]
        public string StreamingAssetsBase = "Localisation";

        [Tooltip("The default namespace to use in each sheet, can be overriden in the sheet Scriptable Object")]
        public string DefaultNamespace = "GameName.Localisation";

        [Tooltip("Not required, but can generate all assets if the LocalisationSheetAsset is referenced here")]
        public LocalisationSheetAsset[] SheetAssets;
    }
}