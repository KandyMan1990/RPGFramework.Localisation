using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// A binding showing the entry <see cref="Key" /> names in an element's property, its <c>text</c> mostly: text that
    /// does not change. Given its text by <see cref="LocalisedTexts.Show" />. For text the code chooses, see
    /// <see cref="PickedLocalisedText" />.
    /// </summary>
    [UxmlObject]
    public sealed partial class LocalisedText : CustomBinding, ILocalisedBinding
    {
        /// <summary>The entry shown, as <c>Sheet/Key</c>.</summary>
        [UxmlAttribute, LocalisationKey]
        public string Key { get; set; }

        string ILocalisedBinding.SheetName => Key.Substring(0, Key.IndexOf('/'));

        private string m_Text;

        public LocalisedText()
        {
            m_Text        = null;
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        void ILocalisedBinding.Show(ILocalisationService localisation)
        {
            m_Text = localisation.Get(Key);

            MarkDirty();
        }

        protected override BindingResult Update(in BindingContext context)
        {
            return LocalisedBinding.Write(context, m_Text);
        }
    }
}
