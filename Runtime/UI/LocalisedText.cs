using Unity.Properties;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>
    /// A binding showing localised text in an element's property, its <c>text</c> mostly: the entry
    /// <see cref="Key" /> names, or, for text the code chooses, the entry <see cref="Hash" /> names in
    /// <see cref="Sheet" />. Given its text by <see cref="LocalisedTexts.Show" />.
    /// </summary>
    [UxmlObject]
    public sealed partial class LocalisedText : CustomBinding
    {
        /// <summary>The entry shown, as <c>Sheet/Key</c>, for text that does not change.</summary>
        [UxmlAttribute]
        public string Key { get; set; }

        /// <summary>The sheet the code chooses an entry from, for text the code chooses; empty with a key.</summary>
        [UxmlAttribute]
        public string Sheet { get; set; }

        /// <summary>The entry shown from <see cref="Sheet" />, by its key's hash; none shows nothing.</summary>
        public ulong Hash { get; set; }

        internal string SheetName => string.IsNullOrEmpty(Key) ? Sheet : Key.Substring(0, Key.IndexOf('/'));

        private string m_Text;

        public LocalisedText()
        {
            m_Text        = null;
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        internal void Show(ILocalisationService localisation)
        {
            m_Text = !string.IsNullOrEmpty(Key)              ? localisation.Get(Key)
                   : localisation.TryGet(Hash, out string text) ? text
                                                                : string.Empty;

            MarkDirty();
        }

        protected override BindingResult Update(in BindingContext context)
        {
            // Until shown, the layout's own text stays, so the UI Builder shows it.
            if (m_Text == null)
            {
                return new BindingResult(BindingStatus.Success, null);
            }

            VisualElement element = context.targetElement;

            if (ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, m_Text, out VisitReturnCode code))
            {
                return new BindingResult(BindingStatus.Success, null);
            }

            return new BindingResult(BindingStatus.Failure,
                                     $"{nameof(LocalisedText)} could not set {context.bindingId}: {code}");
        }
    }
}
