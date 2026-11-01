using System;
using System.Collections;
using UnityEditor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Tests.UI
{
    /// <summary>Layouts as UXML builds them: elements whose text is bound to a <see cref="LocalisedText" />.</summary>
    internal static class Layouts
    {
        internal const string LAYOUT_TEXT = "Layout's own";

        private const int MOST_FRAMES = 300;

        internal static Label Label(LocalisedText text)
        {
            Label label = new Label(LAYOUT_TEXT);

            label.SetBinding("text", text);

            return label;
        }

        internal static LocalisedText Keyed(string key)
        {
            return new LocalisedText { Key = key };
        }

        internal static LocalisedText Chosen(string sheet)
        {
            return new LocalisedText { Sheet = sheet };
        }

        /// <summary>A window whose panel updates bindings as the editor ticks, as a menu's does in play.</summary>
        internal static EditorWindow Window(VisualElement layout)
        {
            PanelWindow window = EditorWindow.GetWindow<PanelWindow>();

            window.rootVisualElement.Add(layout);

            return window;
        }

        /// <summary>
        /// Repaints the window each editor frame until <paramref name="done" />: its panel writes bindings as it
        /// repaints, which an editor in the background does seldom.
        /// </summary>
        internal static IEnumerator Repaint(EditorWindow window, Func<bool> done)
        {
            for (int i = 0; i < MOST_FRAMES && !done(); i++)
            {
                window.Repaint();

                yield return null;
            }
        }

        internal sealed class PanelWindow : EditorWindow
        {
        }
    }
}
