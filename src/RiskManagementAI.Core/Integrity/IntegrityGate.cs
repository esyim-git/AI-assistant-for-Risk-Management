namespace RiskManagementAI.Core.Integrity;

/// <summary>Startup gate decision derived from an <see cref="IntegrityResult"/>.</summary>
public enum GateDecision
{
    /// <summary>The application may continue starting.</summary>
    Allow,

    /// <summary>The application must refuse to start (fail-closed).</summary>
    Block
}

/// <summary>
/// Pure, Core-unit-testable startup gate (STAB-WP-03b, ADR-008). Keeps the actual go/no-go logic
/// out of the WPF layer so it can be verified without a UI: the App shell only renders the decision.
/// </summary>
public static class IntegrityGate
{
    /// <summary>
    /// Decide whether startup may proceed. <see cref="IntegrityStatus.FailClosed"/> always blocks.
    /// <see cref="IntegrityStatus.NotVerified"/> (verification never ran) blocks in operational mode
    /// and is only allowed under the explicit dev switch. Ok / DevFallback / Degraded allow.
    /// </summary>
    public static GateDecision Decide(IntegrityResult result, bool devAllow)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            IntegrityStatus.FailClosed => GateDecision.Block,
            IntegrityStatus.NotVerified => devAllow ? GateDecision.Allow : GateDecision.Block,
            _ => GateDecision.Allow
        };
    }
}
