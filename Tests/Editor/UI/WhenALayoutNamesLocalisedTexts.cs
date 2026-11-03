using NUnit.Framework;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Tests.UI
{
    internal sealed class WhenALayoutNamesLocalisedTexts
    {
        private VisualElement m_Layout;
        private VisualElement m_SecondLayout;

        [SetUp]
        public void SetUp()
        {
            m_Layout = new VisualElement();
            m_Layout.Add(Layouts.Label(Layouts.StaticText("Generic/OK")));
            m_Layout.Add(Layouts.Label(Layouts.DynamicText("Locations")));

            VisualElement nested = new VisualElement();
            nested.Add(Layouts.Label(Layouts.StaticText("Generic/Cancel")));
            m_Layout.Add(nested);

            m_SecondLayout = new VisualElement();
            m_SecondLayout.Add(Layouts.Label(Layouts.StaticText("TestField/Test")));
        }

        [Test]
        public void ThenItsSheetsAreThoseItsKeysAndChoicesAreIn()
        {
            CollectionAssert.AreEqual(new[] { "Generic", "Locations" }, new LocalisedTexts(m_Layout).SheetNames);
        }

        [Test]
        public void ThenTheSheetsOfEveryLayoutGivenAreFound()
        {
            CollectionAssert.AreEqual(new[] { "Generic", "Locations", "TestField" },
                                      new LocalisedTexts(m_Layout, m_SecondLayout).SheetNames);
        }
    }
}
