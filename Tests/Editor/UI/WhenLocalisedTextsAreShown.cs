using System.Collections;
using NUnit.Framework;
using RPGFramework.Hashing;
using RPGFramework.Localisation.Tests.Files;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Tests.UI
{
    internal sealed class WhenLocalisedTextsAreShown
    {
        private ILocalisationService m_Service;
        private EditorWindow         m_Window;
        private Label                m_Keyed;
        private Label                m_Chosen;
        private Button               m_Button;
        private PickedLocalisedText  m_Choice;
        private LocalisedTexts       m_Texts;

        [SetUp]
        public void SetUp()
        {
            FakeStreamingAssetLoader assets = StreamingAssets.Generated(StreamingAssets.FILE_PER_LANGUAGE,
                                                                        Sheets.Three());

            m_Service = StreamingAssets.Service(assets, Sheets.ENGLISH);
            StreamingAssets.Run(m_Service.LoadNewLocalisationDataAsync(new[] { "Generic", "Locations" }));

            m_Choice = Layouts.Chosen("Locations");
            m_Keyed  = Layouts.Label(Layouts.Keyed("Generic/OK"));
            m_Chosen = Layouts.Label(m_Choice);
            m_Button = new Button { text = Layouts.LAYOUT_TEXT };
            m_Button.SetBinding("text", Layouts.Keyed("Generic/Cancel"));

            VisualElement layout = new VisualElement();
            layout.Add(m_Keyed);
            layout.Add(m_Chosen);
            layout.Add(m_Button);

            m_Window = Layouts.Window(layout);
            m_Texts  = new LocalisedTexts(layout);
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        [UnityTest]
        public IEnumerator ThenATextShowsItsKeysEntry()
        {
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Keyed.text != Layouts.LAYOUT_TEXT);

            Assert.AreEqual("OK", m_Keyed.text);
        }

        [UnityTest]
        public IEnumerator ThenAButtonShowsItsKeysEntry()
        {
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Button.text != Layouts.LAYOUT_TEXT);

            Assert.AreEqual("Cancel", m_Button.text);
        }

        [UnityTest]
        public IEnumerator ThenATextTheCodeChoosesShowsTheEntryItsHashNames()
        {
            m_Choice.Hash = Fnv1a64.Hash("Locations/Harbour");
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Chosen.text != Layouts.LAYOUT_TEXT);

            Assert.AreEqual("Harbour Town", m_Chosen.text);
        }

        [UnityTest]
        public IEnumerator ThenATextTheCodeHasNotChosenShowsNothing()
        {
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Chosen.text != Layouts.LAYOUT_TEXT);

            Assert.AreEqual(string.Empty, m_Chosen.text);
        }

        [UnityTest]
        public IEnumerator ThenShownAgainItTakesTheLanguageNowRead()
        {
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Keyed.text != Layouts.LAYOUT_TEXT);

            StreamingAssets.Run(m_Service.SetCurrentLanguage(Sheets.FRENCH));
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Keyed.text != "OK");

            Assert.AreEqual("D'accord", m_Keyed.text);
        }

        [UnityTest]
        public IEnumerator ThenATextNotYetShownKeepsItsLayoutsText()
        {
            Label unshown = Layouts.Label(Layouts.Keyed("Generic/OK"));
            m_Window.rootVisualElement.Add(unshown);

            // The shown button arriving says the panel has written its bindings.
            m_Texts.Show(m_Service);

            yield return Layouts.Repaint(m_Window, () => m_Button.text != Layouts.LAYOUT_TEXT);

            Assert.AreEqual(Layouts.LAYOUT_TEXT, unshown.text);
        }
    }
}
