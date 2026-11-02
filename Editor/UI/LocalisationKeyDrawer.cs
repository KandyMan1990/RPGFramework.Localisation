using UnityEditor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>A key chosen from those the project's sheets recorded at their last pull.</summary>
    [CustomPropertyDrawer(typeof(LocalisationKeyAttribute))]
    internal sealed class LocalisationKeyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return SheetChoiceField.Create(property, ProjectSheetKeys.Find().Keys,
                                           "No sheet holds this key at its last pull: pull, or choose another.");
        }
    }
}
