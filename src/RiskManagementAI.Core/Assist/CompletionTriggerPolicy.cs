namespace RiskManagementAI.Core.Assist;

public sealed record CompletionTriggerDecision(bool ShouldShow, bool ShouldClose, string Reason);

public static class CompletionTriggerPolicy
{
    public const int DefaultMinimumPrefixLength = 2;

    public static CompletionTriggerDecision EvaluateAsYouType(
        CompletionLanguage language,
        string text,
        string prefix,
        int matchCount,
        bool suppressed,
        int minimumPrefixLength = DefaultMinimumPrefixLength)
    {
        if (minimumPrefixLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPrefixLength), "Minimum prefix length must be positive.");
        }

        if (suppressed)
        {
            return Close("SUPPRESSED");
        }

        if (!Enum.IsDefined(language))
        {
            return Close("UNSUPPORTED_LANGUAGE");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Close("EMPTY_TEXT");
        }

        if ((prefix ?? string.Empty).Trim().Length < minimumPrefixLength)
        {
            return Close("PREFIX_TOO_SHORT");
        }

        if (matchCount <= 0)
        {
            return Close("NO_MATCHES");
        }

        return new CompletionTriggerDecision(true, false, "SHOW");
    }

    public static bool ShouldShowExplicitInvocation(int matchCount)
    {
        return matchCount > 0;
    }

    private static CompletionTriggerDecision Close(string reason)
    {
        return new CompletionTriggerDecision(false, true, reason);
    }
}
