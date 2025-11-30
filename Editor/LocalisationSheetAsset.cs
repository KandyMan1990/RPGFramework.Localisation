using UnityEditor;
using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    [CreateAssetMenu(menuName = "RPG Framework/Localisation/Localisation Sheet", fileName = "LocalisationSheet")]
    public class LocalisationSheetAsset : ScriptableObject
    {
        [Header("Master")]
        public LocalisationMaster Master;

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

        public void Generate()
        {
            if (Master == null)
            {
                EditorUtility.DisplayDialog("Missing Master", "Set LocalisationMaster first", "OK");
            }
            else if (string.IsNullOrEmpty(SheetName))
            {
                EditorUtility.DisplayDialog("Missing Sheet Name", "Set SheetName on the asset (for folder naming)", "OK");
            }
            else if (string.IsNullOrEmpty(Gid))
            {
                EditorUtility.DisplayDialog("Missing Gid", "Set Gid on the asset", "OK");
            }
            else
            {
                LocBinGenerator.Generate(this);
            }
        }
    }
}