using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// Every <see cref="LocalisedText" /> in a layout: the sheets they are in, to load before it is shown, and their
    /// text in the current language.
    /// </summary>
    public sealed class LocalisedTexts
    {
        /// <summary>The sheets the texts are in, each named once.</summary>
        public string[] SheetNames => m_SheetNames;

        private readonly LocalisedText[] m_Texts;
        private readonly string[]        m_SheetNames;

        /// <summary>Finds every localised text in <paramref name="roots" /> and the elements beneath them.</summary>
        public LocalisedTexts(params VisualElement[] roots)
        {
            m_Texts      = FindTexts(roots);
            m_SheetNames = FindSheetNames(m_Texts);
        }

        /// <summary>
        /// Gives every text its words in <paramref name="localisation" />'s language, which the panel shows on its next
        /// update. Their sheets must be loaded.
        /// </summary>
        public void Show(ILocalisationService localisation)
        {
            for (int i = 0; i < m_Texts.Length; i++)
            {
                m_Texts[i].Show(localisation);
            }
        }

        // Counting them first would take the same walk, so they are listed as found.
        private static LocalisedText[] FindTexts(VisualElement[] roots)
        {
            List<VisualElement> elements = new List<VisualElement>();

            for (int i = 0; i < roots.Length; i++)
            {
                roots[i].Query<VisualElement>().ToList(elements);
            }

            List<BindingInfo>   bindings = new List<BindingInfo>();
            List<LocalisedText> texts    = new List<LocalisedText>();

            for (int i = 0; i < elements.Count; i++)
            {
                bindings.Clear();
                elements[i].GetBindingInfos(bindings);

                for (int j = 0; j < bindings.Count; j++)
                {
                    if (bindings[j].binding is LocalisedText text)
                    {
                        texts.Add(text);
                    }
                }
            }

            return texts.ToArray();
        }

        private static string[] FindSheetNames(LocalisedText[] texts)
        {
            string[] names = new string[texts.Length];
            int      count = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                string name = texts[i].SheetName;

                if (Array.IndexOf(names, name, 0, count) < 0)
                {
                    names[count++] = name;
                }
            }

            string[] sheetNames = new string[count];
            Array.Copy(names, sheetNames, count);

            return sheetNames;
        }
    }
}
