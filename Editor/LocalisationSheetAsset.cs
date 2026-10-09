using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    public class LocalisationSheetAsset : ScriptableObject
    {
        [Header("Keys class")]
        [Tooltip("Generate a C# class holding this sheet's keys as constants, for code that names them. Scripts and the editors read the keys recorded on this asset, and need no class")]
        [SerializeField]
        internal bool GenerateKeysClass;

        [Tooltip("The folder this sheet's generated keys class, a C# script, goes in. Leave this blank to use the folder in master")]
        [SerializeField]
        internal string KeysClassFolderOverride = string.Empty;

        [Tooltip("An override for the default namespace, leave this blank to use the value in master")]
        [SerializeField]
        internal string NamespaceOverride = string.Empty;

        [SerializeField]
        [HideInInspector]
        private string[] m_Keys = Array.Empty<string>();

        /// <summary>The tab's name in the spreadsheet, which the pull names the asset after.</summary>
        public string SheetName => name;

        public IReadOnlyList<string> Keys => m_Keys;

        internal void SetKeys(string[] keys) => m_Keys = keys;
    }
}