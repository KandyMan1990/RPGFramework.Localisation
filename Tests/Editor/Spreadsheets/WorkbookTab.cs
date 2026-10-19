namespace RPGFramework.Localisation.Tests.Spreadsheets
{
    /// <summary>A tab of a workbook: its name, where its sheet's XML is, relative to <c>xl/</c> or absolute, and the XML.</summary>
    internal readonly struct WorkbookTab
    {
        internal readonly string Name;
        internal readonly string Target;
        internal readonly string Xml;

        internal WorkbookTab(string name, string target, string xml)
        {
            Name   = name;
            Target = target;
            Xml    = xml;
        }
    }
}
