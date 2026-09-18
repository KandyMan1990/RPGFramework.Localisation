using System.Collections.Generic;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;

namespace RPGFramework.Localisation.Editor
{
    internal interface ILocalisationBinGenerator
    {
        /// <summary>
        /// Builds every locbin in memory.
        /// </summary>
        List<LocalisationBinFile> BuildLocalisationBin(List<LocalisationSheetContent> dataToWrite);
    }

    /// <summary>
    /// Checks a sheet's text as it arrives, before anything is written.<br /><br />
    /// Localisation does not know what its text is used for, so a package that gives text a meaning — dialogue
    /// markup, say — implements this to have its mistakes reported when the sheets are generated, which is when
    /// the writer can still fix them. Every implementation in the project is found and run; it needs a
    /// parameterless constructor.
    /// </summary>
    public interface ILocalisationTextValidator
    {
        /// <param name="problems">Add a readable line per mistake, naming the sheet, language and key.</param>
        void Validate(string sheetName, string language, string key, string text, List<string> problems);
    }
}
