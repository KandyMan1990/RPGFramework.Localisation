using UnityEditor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>A sheet chosen from the project's.</summary>
    [CustomPropertyDrawer(typeof(LocalisationSheetAttribute))]
    internal sealed class LocalisationSheetDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return SheetChoiceField.Create(property, ProjectSheetKeys.Find().SheetNames,
                                           "The project has no sheet of this name: pull, or choose another.");
        }
    }
}
