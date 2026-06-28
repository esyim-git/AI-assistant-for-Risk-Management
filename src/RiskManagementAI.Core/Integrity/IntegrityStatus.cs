namespace RiskManagementAI.Core.Integrity;

/// <summary>
/// Runtime integrity verification outcome (STAB-WP-03b, ADR-008).
/// Mirrors the build/03 manifest verification decision in-process at startup.
/// </summary>
public enum IntegrityStatus
{
    /// <summary>Manifest present and every declared entry matched (hash + size). Safe to run.</summary>
    Ok,

    /// <summary>
    /// Reserved for a future per-domain partial-degradation mode (some classes blocked, app still
    /// usable for the rest). Not produced by the STAB-WP-03b interim verifier.
    /// </summary>
    Degraded,

    /// <summary>
    /// Verification failed but an explicit, package-absent developer switch was active, so the
    /// failure is downgraded to warnings and the app proceeds. Never reachable from a release package.
    /// </summary>
    DevFallback,

    /// <summary>Operational (release) mode and verification failed. The app must refuse to start.</summary>
    FailClosed,

    /// <summary>Verification has not run yet (initial state before startup).</summary>
    NotVerified
}
