using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Localisation.Sample.Editor
{
    /// <summary>
    /// The text is read from StreamingAssets, which a sample cannot put files in when it is imported, so this copies the
    /// sample's own there. It refuses a project that already has localisation files rather than replace a game's.
    /// </summary>
    internal static class InstallSampleText
    {
        private const string DESTINATION = "Assets/StreamingAssets/Localisation";
        private const string MANIFEST    = "manifest.locman";

        [MenuItem("RPG Framework/Localisation/Install Sample Text")]
        private static void Install()
        {
            if (File.Exists(Path.Combine(DESTINATION, MANIFEST)))
            {
                EditorUtility.DisplayDialog("Localisation already installed", $"{DESTINATION} already holds localisation files, and the sample's would replace them. Try the sample in a project without its own, or move them aside first.", "OK");
                return;
            }

            string source = FindSampleText();

            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];

                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                string target = Path.Combine(DESTINATION, Path.GetRelativePath(source, file));

                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target);
            }

            AssetDatabase.Refresh();

            Debug.Log($"Copied the Localisation Sample's text to {DESTINATION}");
        }

        private static string FindSampleText()
        {
            string[] guids      = AssetDatabase.FindAssets($"{nameof(InstallSampleText)} t:MonoScript");
            string   scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string   text       = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)), "Text");

            return text;
        }
    }
}
