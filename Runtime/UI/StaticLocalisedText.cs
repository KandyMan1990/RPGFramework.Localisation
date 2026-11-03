using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// A binding showing the entry <see cref="Key" /> names in an element's property, its <c>text</c> mostly: the key is
    /// fixed in the layout, the text following the language. Given its text by <see cref="LocalisedTexts.Show" />. For
    /// text the code chooses, see <see cref="DynamicLocalisedText" />.
    /// </summary>
    [UxmlObject]
    public sealed partial class StaticLocalisedText : CustomBinding, ILocalisedBinding
    {
        /// <summary>The entry shown, as <c>Sheet/Key</c>.</summary>
        [UxmlAttribute, LocalisationKey]
        public string Key { get; set; }

        string ILocalisedBinding.SheetName => Key.Substring(0, Key.IndexOf('/'));

        private string m_Text;

        public StaticLocalisedText()
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
