using System.Collections.Generic;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal readonly struct SpreadsheetTab
    {
        internal readonly string         Name;
        internal readonly List<string[]> Rows;

        internal SpreadsheetTab(string name, List<string[]> rows)
        {
            Name = name;
            Rows = rows;
        }
    }
}
