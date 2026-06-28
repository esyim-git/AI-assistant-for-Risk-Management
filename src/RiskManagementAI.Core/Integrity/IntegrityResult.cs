using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Integrity;

/// <summary>
/// Result of a runtime integrity verification pass (STAB-WP-03b, ADR-008).
/// Shape mirrors <see cref="RiskManagementAI.Core.Config.PolicyLoadResult"/>: an outcome status,
/// a fallback flag, audit-friendly warnings, surfaceable safety findings, and the set of blocked
/// security classes. Messages expose hash <em>prefixes only</em> (never file contents).
/// </summary>
public sealed record IntegrityResult(
    IntegrityStatus Status,
    bool UsedDevFallback,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SafetyFinding> Findings,
    IReadOnlySet<string> BlockedClasses)
{
    /// <summary>Initial state before <see cref="IntegrityVerifier.VerifyPackage"/> has run.</summary>
    public static IntegrityResult NotVerified() => new(
        IntegrityStatus.NotVerified,
        UsedDevFallback: false,
        Warnings: new[] { "Integrity has not been verified yet." },
        Findings: Array.Empty<SafetyFinding>(),
        BlockedClasses: new HashSet<string>(StringComparer.Ordinal));

    /// <summary>
    /// Audit-safe hash representation: the uppercase 12-char prefix only. Verification messages
    /// must never echo full hashes or file contents (security review: hash-prefix only).
    /// </summary>
    public static string ShortHash(string? sha256)
    {
        if (string.IsNullOrEmpty(sha256))
        {
            return "(none)";
        }

        var normalized = sha256.ToUpperInvariant();
        return normalized.Length <= 12 ? normalized : normalized.Substring(0, 12);
    }
}
