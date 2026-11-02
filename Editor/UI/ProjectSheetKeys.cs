using System;
using UnityEditor;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>
    /// The keys, <c>Sheet/Key</c>, and the sheets the project's sheet assets recorded at their last pull.
    /// </summary>
    internal sealed class ProjectSheetKeys
    {
        internal string[] Keys       => m_Keys;
        internal string[] SheetNames => m_SheetNames;

        private readonly string[] m_Keys;
        private readonly string[] m_SheetNames;

        internal ProjectSheetKeys(string[] keys, string[] sheetNames)
        {
            m_Keys       = keys;
            m_SheetNames = sheetNames;
        }

        internal static ProjectSheetKeys Find()
        {
            string[]                 guids  = AssetDatabase.FindAssets("t:" + nameof(LocalisationSheetAsset));
            LocalisationSheetAsset[] sheets = new LocalisationSheetAsset[guids.Length];
            string[]                 names  = new string[guids.Length];
            int                      count  = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                sheets[i] = AssetDatabase.LoadAssetAtPath<LocalisationSheetAsset>(path);
                names[i]  = sheets[i].SheetName;
                count    += sheets[i].Keys.Count;
            }

            string[] keys = new string[count];
            int      next = 0;

            for (int i = 0; i < sheets.Length; i++)
            {
                for (int j = 0; j < sheets[i].Keys.Count; j++)
                {
                    keys[next++] = sheets[i].Keys[j];
                }
            }

            return new ProjectSheetKeys(keys, names);
        }

        internal bool HasKey(string key) => Array.IndexOf(m_Keys, key) >= 0;

        internal bool HasSheet(string sheetName) => Array.IndexOf(m_SheetNames, sheetName) >= 0;
    }
}
