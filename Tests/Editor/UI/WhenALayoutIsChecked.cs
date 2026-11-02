using NUnit.Framework;
using RPGFramework.Localisation.Editor;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Tests.UI
{
    internal sealed class WhenALayoutIsChecked
    {
        private const string LAYOUT = "Title.uxml";

        private ProjectSheetKeys m_Keys;

        [SetUp]
        public void SetUp()
        {
            m_Keys = new ProjectSheetKeys(new[] { "Generic/OK", "Generic/Cancel", "Locations/Harbour" },
                                          new[] { "Generic", "Locations" });
        }

        [Test]
        public void ThenKeysAndSheetsTheSheetsHoldAreNotReported()
        {
            VisualElement layout = Layout(Layouts.Keyed("Generic/OK"), Layouts.Chosen("Locations"));

            Assert.IsEmpty(LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenAKeyNoSheetHoldsIsReported()
        {
            VisualElement layout = Layout(Layouts.Keyed("Generic/OK"), Layouts.Keyed("Generic/Gone"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: no sheet holds the key Generic/Gone" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenASheetTheGameDoesNotShipIsReported()
        {
            VisualElement layout = Layout(Layouts.Chosen("Nowhere"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: there is no sheet Nowhere" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenATextNamingNoKeyIsReported()
        {
            VisualElement layout = Layout(new LocalisedText());

            CollectionAssert.AreEqual(new[] { "Title.uxml: a localised text names no key" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenAKeyNotNamingItsSheetIsReported()
        {
            VisualElement layout = Layout(Layouts.Keyed("OK"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: the key OK does not name its sheet, as Sheet/Key" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenAPickedTextNamingNoSheetIsReported()
        {
            VisualElement layout = Layout(new PickedLocalisedText());

            CollectionAssert.AreEqual(new[] { "Title.uxml: a picked localised text names no sheet" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        private static VisualElement Layout(params Binding[] texts)
        {
            VisualElement layout = new VisualElement();

            for (int i = 0; i < texts.Length; i++)
            {
                layout.Add(Layouts.Label(texts[i]));
            }

            return layout;
        }
    }
}
