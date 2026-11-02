using Unity.Properties;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation
{
    /// <summary>What the localised bindings share: writing their text to the property they are bound to.</summary>
    internal static class LocalisedBinding
    {
        /// <summary>
        /// Writes <paramref name="text" /> to the bound property; until shown, writes nothing, so the layout's own text
        /// stays and the UI Builder shows it.
        /// </summary>
        internal static BindingResult Write(in BindingContext context, string text)
        {
            if (text == null)
            {
                return new BindingResult(BindingStatus.Success, null);
            }

            VisualElement element = context.targetElement;

            if (ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, text, out VisitReturnCode code))
            {
                return new BindingResult(BindingStatus.Success, null);
            }

            return new BindingResult(BindingStatus.Failure, $"Localised text could not set {context.bindingId}: {code}");
        }
    }
}
