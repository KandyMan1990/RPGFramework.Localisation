using System.IO;
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
using System.Text;
#endif

namespace RPGFramework.Localisation.Helpers
{
    internal static class HelperFunctions
    {
        private static readonly char[] m_LanguageSeparators =
        {
            '-',
            '_'
        };

        internal static string CombinePath(params string[] parts)
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(part.TrimEnd('/'));
                    continue;
                }

                builder.Append('/');
                builder.Append(part.Trim('/'));
            }

            return builder.ToString();
#else
            return Path.Combine(parts);
#endif
        }

        internal static string GetNeutralLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return language;
            }

            int index = language.IndexOfAny(m_LanguageSeparators);

            return index > 0 ? language[..index] : language;
        }
    }
}