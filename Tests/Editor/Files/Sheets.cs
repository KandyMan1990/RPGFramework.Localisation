using System.Collections.Generic;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;

namespace RPGFramework.Localisation.Tests.Files
{
    /// <summary>Sheets as the spreadsheet's tabs give them: a name, its languages, its keys and each language's text.</summary>
    internal static class Sheets
    {
        internal const string ENGLISH = "en-GB";
        internal const string FRENCH  = "fr-FR";

        internal static LocalisationSheetContent Sheet(string name, string[] languages, string[] keys, params string[][] values)
        {
            Dictionary<string, List<string>> byLanguage = new Dictionary<string, List<string>>();

            for (int i = 0; i < languages.Length; i++)
            {
                byLanguage[languages[i]] = new List<string>(values[i]);
            }

            LocalisationSheetContent sheet = new LocalisationSheetContent(name, new List<string>(languages), new List<string>(keys), byLanguage);

            return sheet;
        }

        /// <summary>Three sheets in English and French, so the one in the middle starts at an offset neither end hides.</summary>
        internal static List<LocalisationSheetContent> Three()
        {
            string[] languages = { ENGLISH, FRENCH };

            List<LocalisationSheetContent> sheets = new List<LocalisationSheetContent>
                                                    {
                                                        Sheet("Generic", languages, new[] { "OK", "Cancel" }, new[] { "OK", "Cancel" }, new[] { "D'accord", "Annuler" }),
                                                        Sheet("TestField", languages, new[] { "Test", "Greeting" }, new[] { "Héllo wörld — ✓", "日本語のテキスト" }, new[] { "Bonjour", "Salut" }),
                                                        Sheet("Locations", languages, new[] { "Harbour" }, new[] { "Harbour Town" }, new[] { "Ville du port" })
                                                    };

            return sheets;
        }
    }
}
