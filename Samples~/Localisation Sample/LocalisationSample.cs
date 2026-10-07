using System;
using RPGFramework.Hashing;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Sample
{
    /// <summary>
    /// A title screen and a settings panel in five languages. Generic stays loaded while the sample runs, each panel
    /// loads its own sheet as it opens and unloads it as it closes, and every label is redrawn when the language changes.
    /// </summary>
    public sealed class LocalisationSample : MonoBehaviour
    {
        [SerializeField] private UIDocument m_Document;

        private ILocalisationService m_Localisation;

        private Label         m_Title;
        private VisualElement m_TitlePanel;
        private Button        m_NewGame;
        private Button        m_LoadGame;
        private Button        m_QuitGame;
        private VisualElement m_SettingsPanel;
        private Label         m_LanguageTitle;
        private Button        m_Language;
        private Label         m_MusicVolume;
        private Label         m_SfxVolume;
        private Button        m_Settings;

        private bool m_SettingsOpen;

        private async void Start()
        {
            BuildUI();

            m_Localisation = new LocalisationService();

            try
            {
                await m_Localisation.InitialiseAsync();
            }
            catch (Exception)
            {
                m_Title.text = "Install the sample's text first:\nRPG Framework > Localisation > Install Sample Text";
                return;
            }

            m_Localisation.OnLanguageChanged += Redraw;

            await m_Localisation.LoadNewLocalisationDataAsync(new[] { LocalisationKeys.Generic.SHEET_NAME, LocalisationKeys.BeginMenu.SHEET_NAME });

            Redraw(m_Localisation.CurrentLanguage);
            m_Settings.SetEnabled(true);
        }

        private void OnDestroy()
        {
            if (m_Localisation == null)
            {
                return;
            }

            m_Localisation.OnLanguageChanged -= Redraw;
            m_Localisation.UnloadAllLocalisationData();
        }

        private async void ToggleSettings()
        {
            m_Settings.SetEnabled(false);
            m_SettingsOpen = !m_SettingsOpen;

            string opening = m_SettingsOpen ? LocalisationKeys.ConfigMenu.SHEET_NAME : LocalisationKeys.BeginMenu.SHEET_NAME;
            string closing = m_SettingsOpen ? LocalisationKeys.BeginMenu.SHEET_NAME  : LocalisationKeys.ConfigMenu.SHEET_NAME;

            await m_Localisation.LoadNewLocalisationDataAsync(opening);
            m_Localisation.UnloadLocalisationData(closing);

            m_TitlePanel.style.display    = m_SettingsOpen ? DisplayStyle.None : DisplayStyle.Flex;
            m_SettingsPanel.style.display = m_SettingsOpen ? DisplayStyle.Flex : DisplayStyle.None;

            Redraw(m_Localisation.CurrentLanguage);
            m_Settings.SetEnabled(true);
        }

        private async void NextLanguage()
        {
            m_Language.SetEnabled(false);

            string[] languages = await m_Localisation.GetAllLanguages();
            int      current   = Array.IndexOf(languages, m_Localisation.CurrentLanguage);

            await m_Localisation.SetCurrentLanguage(languages[(current + 1) % languages.Length]);

            m_Language.SetEnabled(true);
        }

        private void Redraw(string language)
        {
            m_Title.text    = m_Localisation.Get(LocalisationKeys.Generic.GAME_TITLE);
            m_Settings.text = m_Localisation.Get(LocalisationKeys.Generic.SETTINGS);

            if (m_SettingsOpen)
            {
                m_LanguageTitle.text = m_Localisation.Get(LocalisationKeys.ConfigMenu.LANGUAGE_TITLE);
                m_Language.text      = m_Localisation.Get(LocalisationKeys.ConfigMenu.LANGUAGE);
                m_MusicVolume.text   = m_Localisation.Get(LocalisationKeys.ConfigMenu.MUSIC_VOLUME);
                m_SfxVolume.text     = m_Localisation.Get(LocalisationKeys.ConfigMenu.SFX_VOLUME);
                return;
            }

            m_NewGame.text  = m_Localisation.Get(LocalisationKeys.BeginMenu.NEW_GAME);
            m_LoadGame.text = m_Localisation.Get(LocalisationKeys.BeginMenu.LOAD_GAME);
            m_QuitGame.text = m_Localisation.Get(LocalisationKeys.BeginMenu.QUIT_GAME);
        }

        private void BuildUI()
        {
            VisualElement root = m_Document.rootVisualElement;
            root.style.position       = Position.Absolute;
            root.style.left           = 0;
            root.style.right          = 0;
            root.style.top            = 0;
            root.style.bottom         = 0;
            root.style.alignItems     = Align.Center;
            root.style.justifyContent = Justify.Center;

            m_Title = new Label();
            m_Title.style.fontSize                = 72;
            m_Title.style.color                   = Color.white;
            m_Title.style.marginBottom            = 48;
            m_Title.style.unityTextAlign          = TextAnchor.MiddleCenter;
            root.Add(m_Title);

            m_TitlePanel = AddPanel(root);
            m_NewGame    = AddButton(m_TitlePanel);
            m_LoadGame   = AddButton(m_TitlePanel);
            m_QuitGame   = AddButton(m_TitlePanel);

            // Text can be looked up by its key's hash, which is what a save or an asset would keep.
            m_NewGame.clicked += () => Debug.Log(m_Localisation.Get(Fnv1a64.Hash(LocalisationKeys.BeginMenu.NEW_GAME)));

            m_SettingsPanel               = AddPanel(root);
            m_SettingsPanel.style.display = DisplayStyle.None;
            m_LanguageTitle               = AddLabel(m_SettingsPanel);
            m_Language                    = AddButton(m_SettingsPanel);
            m_MusicVolume                 = AddLabel(m_SettingsPanel);
            m_SfxVolume                   = AddLabel(m_SettingsPanel);
            m_Language.clicked           += NextLanguage;

            m_Settings = AddButton(root);
            m_Settings.style.marginTop = 48;
            m_Settings.clicked        += ToggleSettings;
            m_Settings.SetEnabled(false);
        }

        private static VisualElement AddPanel(VisualElement parent)
        {
            VisualElement panel = new VisualElement();
            panel.style.alignItems = Align.Stretch;
            panel.style.width      = 480;
            parent.Add(panel);

            return panel;
        }

        private static Button AddButton(VisualElement parent)
        {
            Button button = new Button();
            button.style.fontSize = 32;
            button.style.height   = 64;
            parent.Add(button);

            return button;
        }

        private static Label AddLabel(VisualElement parent)
        {
            Label label = new Label();
            label.style.fontSize       = 32;
            label.style.color          = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop      = 16;
            parent.Add(label);

            return label;
        }
    }
}
