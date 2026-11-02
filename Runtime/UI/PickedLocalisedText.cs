using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// A binding showing an entry the code picks from <see cref="Sheet" />, by <see cref="Hash" />, in an element's
    /// property, its <c>text</c> mostly: a place's name kept as its key's hash, say. The layout names only the sheet, so
    /// it is loaded. Given its text by <see cref="LocalisedTexts.Show" />.
    /// </summary>
    [UxmlObject]
    public sealed partial class PickedLocalisedText : CustomBinding, ILocalisedBinding
    {
        /// <summary>The sheet the code picks an entry from.</summary>
        [UxmlAttribute, LocalisationSheet]
        public string Sheet { get; set; }

        /// <summary>The entry shown, by its key's hash; none shows nothing.</summary>
        public ulong Hash { get; set; }

        string ILocalisedBinding.SheetName => Sheet;

        private string m_Text;

        public PickedLocalisedText()
        {
            m_Text        = null;
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        void ILocalisedBinding.Show(ILocalisationService localisation)
        {
            m_Text = localisation.TryGet(Hash, out string text) ? text : string.Empty;

            MarkDirty();
        }

        protected override BindingResult Update(in BindingContext context)
        {
            return LocalisedBinding.Write(context, m_Text);
        }
    }
}
