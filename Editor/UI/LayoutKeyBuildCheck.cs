using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RPGFramework.Localisation.Editor
{
    /// <summary>
    /// Refuses to build a player whose layouts name a key no sheet holds, or a sheet the game does not ship, which at
    /// run time would show as missing or stop the screen opening.
    /// </summary>
    internal sealed class LayoutKeyBuildCheck : IPreprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            string[] problems = LayoutKeyCheck.FindProblemsInProject();

            if (problems.Length > 0)
            {
                throw new BuildFailedException($"The layouts name {problems.Length} localisation key(s) or sheet(s) " +
                                               $"the sheets do not hold:\n  {string.Join("\n  ", problems)}");
            }
        }
    }
}
