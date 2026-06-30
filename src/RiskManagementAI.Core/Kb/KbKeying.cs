using System.Text;

namespace RiskManagementAI.Core.Kb;

internal static class KbKeying
{
    internal const int MaxSubstringKeyLength = 32;

    internal static IEnumerable<string> TextKeys(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            yield break;
        }

        if (normalized.Length <= MaxSubstringKeyLength)
        {
            yield return normalized;
        }

        foreach (var term in SplitTerms(normalized))
        {
            if (term.Length <= MaxSubstringKeyLength)
            {
                yield return term;
            }
        }

        foreach (var ngram in BoundedSubstrings(normalized))
        {
            yield return ngram;
        }
    }

    internal static bool RequiresLinearContainsFallback(string query)
    {
        return query.Length > MaxSubstringKeyLength;
    }

    internal static IEnumerable<string> SplitTerms(string value)
    {
        var current = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                current.Append(ch);
                continue;
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static IEnumerable<string> BoundedSubstrings(string value)
    {
        for (var start = 0; start < value.Length; start++)
        {
            var maxLength = Math.Min(MaxSubstringKeyLength, value.Length - start);
            for (var length = 1; length <= maxLength; length++)
            {
                yield return value.Substring(start, length);
            }
        }
    }
}
