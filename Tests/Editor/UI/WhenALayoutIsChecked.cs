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
            VisualElement layout = Layout(Layouts.StaticText("Generic/OK"), Layouts.DynamicText("Locations"));

            Assert.IsEmpty(LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenAKeyNoSheetHoldsIsReported()
        {
            VisualElement layout = Layout(Layouts.StaticText("Generic/OK"), Layouts.StaticText("Generic/Gone"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: no sheet holds the key Generic/Gone" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenASheetTheGameDoesNotShipIsReported()
        {
            VisualElement layout = Layout(Layouts.DynamicText("Nowhere"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: there is no sheet Nowhere" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenATextNamingNoKeyIsReported()
        {
            VisualElement layout = Layout(new StaticLocalisedText());

            CollectionAssert.AreEqual(new[] { "Title.uxml: a static localised text names no key" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenAKeyNotNamingItsSheetIsReported()
        {
            VisualElement layout = Layout(Layouts.StaticText("OK"));

            CollectionAssert.AreEqual(new[] { "Title.uxml: the key OK does not name its sheet, as Sheet/Key" },
                                      LayoutKeyCheck.FindProblems(LAYOUT, layout, m_Keys));
        }

        [Test]
        public void ThenADynamicTextNamingNoSheetIsReported()
        {
            VisualElement layout = Layout(new DynamicLocalisedText());

            CollectionAssert.AreEqual(new[] { "Title.uxml: a dynamic localised text names no sheet" },
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
