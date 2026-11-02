using System.Diagnostics;
using UnityEngine;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// Marks a string as a key, <c>Sheet/Key</c>, which the inspector offers from the keys the project's sheets
    /// recorded at their last pull. Left out of a player build: only the editor reads it.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public sealed class LocalisationKeyAttribute : PropertyAttribute
    {
    }
}
