using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>
    /// A string chosen from what the project's sheets recorded: keys show grouped by sheet, as the menu reads
    /// <c>Sheet/Key</c>. A value none of them holds is kept, and said to be missing, rather than cleared.
    /// </summary>
    internal static class SheetChoiceField
    {
        private const string NONE = "(None)";

        internal static VisualElement Create(SerializedProperty property, string[] choices, string missing)
        {
            List<string> options = new List<string>(choices.Length + 1) { NONE };
            options.AddRange(choices);

            DropdownField field   = new DropdownField(property.displayName, options, 0) { tooltip = property.tooltip };
            HelpBox       warning = new HelpBox(missing, HelpBoxMessageType.Warning);

            field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            field.SetValueWithoutNotify(string.IsNullOrEmpty(property.stringValue) ? NONE : property.stringValue);
            field.RegisterValueChangedCallback(e =>
            {
                property.stringValue = e.newValue == NONE ? string.Empty : e.newValue;
                property.serializedObject.ApplyModifiedProperties();

                ShowIfMissing(warning, property.stringValue, choices);
            });

            ShowIfMissing(warning, property.stringValue, choices);

            VisualElement container = new VisualElement();
            container.Add(field);
            container.Add(warning);

            return container;
        }

        private static void ShowIfMissing(HelpBox warning, string value, string[] choices)
        {
            bool missing = !string.IsNullOrEmpty(value) && Array.IndexOf(choices, value) < 0;

            warning.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
