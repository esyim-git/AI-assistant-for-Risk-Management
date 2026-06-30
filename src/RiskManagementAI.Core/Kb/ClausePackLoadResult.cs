using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Kb;

public sealed record ClausePackLoadResult(
    IReadOnlyList<RegulationClause> Clauses,
    bool UsedFallback,
    IReadOnlyList<SafetyFinding> Findings);
