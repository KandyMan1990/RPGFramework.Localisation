using System.Collections.Generic;
using RPGFramework.Localisation.Editor.LocalisationBinWriter;

namespace RPGFramework.Localisation.Editor
{
    internal interface ILocalisationBinWriter
    {
        void GenerateLocalisationBin(List<LocalisationSheetContent> dataToWrite);
    }
}