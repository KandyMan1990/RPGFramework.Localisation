using System.Diagnostics;
using UnityEngine;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// Marks a string as a sheet's name, which the inspector offers from the project's sheets. Left out of a player
    /// build: only the editor reads it.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public sealed class LocalisationSheetAttribute : PropertyAttribute
    {
    }
}
