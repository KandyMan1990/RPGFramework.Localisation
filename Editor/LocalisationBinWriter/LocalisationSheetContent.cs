using System.Collections.Generic;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal readonly struct LocalisationSheetContent
    {
        internal readonly string                           SheetName;
        internal readonly List<string>                     Languages;
        internal readonly List<string>                     Keys;
        internal readonly Dictionary<string, List<string>> Values;

        internal LocalisationSheetContent(string sheetName, List<string> languages, List<string> keys, Dictionary<string, List<string>> values)
        {
            SheetName = sheetName;
            Languages = languages;
            Keys      = keys;
            Values    = values;
        }
    }
}