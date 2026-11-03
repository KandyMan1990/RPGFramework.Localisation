using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>
    /// What a layout's localised texts name that the sheets do not hold: at run time loading a sheet the game does not
    /// ship throws, and a key no sheet holds shows as missing.
    /// </summary>
    internal static class LayoutKeyCheck
    {
        /// <summary>
        /// The problems of every layout in the game's assets, the packages' left out, as no game edits them.
        /// </summary>
        internal static string[] FindProblemsInProject()
        {
            ProjectSheetKeys keys     = ProjectSheetKeys.Find();
            string[]         guids    = AssetDatabase.FindAssets("t:" + nameof(VisualTreeAsset), new[] { "Assets" });
            List<string>     problems = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string          path   = AssetDatabase.GUIDToAssetPath(guids[i]);
                VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);

                problems.AddRange(FindProblems(path, layout.Instantiate(), keys));
            }

            return problems.ToArray();
        }

        internal static string[] FindProblems(string layoutName, VisualElement layout, ProjectSheetKeys keys)
        {
            ILocalisedBinding[] texts    = LocalisedTexts.FindTexts(new[] { layout });
            List<string>        problems = new List<string>();

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] is StaticLocalisedText staticText)
                {
                    FindKeyProblem(layoutName, staticText.Key, keys, problems);
                }
                else if (texts[i] is DynamicLocalisedText dynamicText)
                {
                    FindSheetProblem(layoutName, dynamicText.Sheet, keys, problems);
                }
            }

            return problems.ToArray();
        }

        private static void FindKeyProblem(string layoutName, string key, ProjectSheetKeys keys, List<string> problems)
        {
            if (string.IsNullOrEmpty(key))
            {
                problems.Add($"{layoutName}: a static localised text names no key");
            }
            else if (key.IndexOf('/') <= 0)
            {
                problems.Add($"{layoutName}: the key {key} does not name its sheet, as Sheet/Key");
            }
            else if (!keys.HasKey(key))
            {
                problems.Add($"{layoutName}: no sheet holds the key {key}");
            }
        }

        private static void FindSheetProblem(string layoutName, string sheet, ProjectSheetKeys keys, List<string> problems)
        {
            if (string.IsNullOrEmpty(sheet))
            {
                problems.Add($"{layoutName}: a dynamic localised text names no sheet");
            }
            else if (!keys.HasSheet(sheet))
            {
                problems.Add($"{layoutName}: there is no sheet {sheet}");
            }
        }
    }
}
