using System.IO;

namespace RPGFramework.Localisation
{
    internal static class Utilities
    {
        internal static string Combine(params string[] parts)
        {
#if UNITY_ANDROID || UNITY_WEBGL
            return string.Join('/', parts.Select(p => p.Trim('/')));
#else
            return Path.Combine(parts);
#endif
        }

        internal static string GetNeutralLanguage(string language)
        {
            int index = language.IndexOfAny(new[]
                                            {
                                                    '-',
                                                    '_'
                                            });

            return index > 0 ? language[..index] : language;
        }
    }
}