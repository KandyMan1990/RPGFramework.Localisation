namespace RPGFramework.Localisation
{
    /// <summary>A binding showing localised text, which <see cref="LocalisedTexts" /> loads the sheet of and shows.</summary>
    internal interface ILocalisedBinding
    {
        /// <summary>The sheet its text is in, which must be loaded before it is shown.</summary>
        string SheetName { get; }

        /// <summary>Takes its text in <paramref name="localisation" />'s language, written on the panel's next update.</summary>
        void Show(ILocalisationService localisation);
    }
}
