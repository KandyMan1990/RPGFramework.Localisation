using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Localisation Sheet", fileName = "LocalisationSheet")]
    public class LocalisationSheetAsset : ScriptableObject
    {
        [Header("Sheet")]
        [Tooltip("Tab name inside the Google Sheet")]
        public string SheetName;

        [Tooltip("gid for the sheet tab")]
        public string Gid;

        [Header("Generation")]
        [Tooltip("Where to drop the generated C# keys file (example: Assets/GeneratedLocalisation/<SheetName>)")]
        public string GeneratedOutputFolder = "Assets/GeneratedLocalisation";

        [Tooltip("An override for the default namespace, leave this blank to use the value in master")]
        public string NamespaceOverride = string.Empty;
    }
}