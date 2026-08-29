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
}