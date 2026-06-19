namespace RiskManagementAI.Core.Config;

public sealed record PolicyLoadResult(
    SecurityPolicy Policy,
    bool UsedFallback,
    IReadOnlyList<string> Warnings
);
