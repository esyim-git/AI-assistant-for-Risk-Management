using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public enum PriorDayMovement
{
    New,
    Resolved,
    Increased,
    Decreased,
    Unchanged,
    StateTransition
}

public sealed record PriorDayComparisonRow(
    string PortfolioId,
    string RiskFactor,
    string CurrentBaseDate,
    string PriorBaseDate,
    LimitMonitorStatus? CurrentStatus,
    LimitMonitorStatus? PriorStatus,
    decimal CurrentUsageRatio,
    decimal PriorUsageRatio,
    decimal UsageRatioDelta,
    decimal CurrentExposureAmount,
    decimal PriorExposureAmount,
    decimal ExposureAmountDelta,
    decimal CurrentLimitAmount,
    decimal PriorLimitAmount,
    decimal LimitAmountDelta,
    decimal CurrentRemainingLimit,
    decimal PriorRemainingLimit,
    decimal RemainingLimitDelta,
    PriorDayMovement Movement);

public sealed record PriorDayKpis(
    int ComparedCount,
    int NewCount,
    int ResolvedCount,
    int IncreasedCount,
    int DecreasedCount,
    int UnchangedCount,
    int StateTransitionCount)
{
    public static PriorDayKpis FromRows(IReadOnlyList<PriorDayComparisonRow> rows)
    {
        return new PriorDayKpis(
            rows.Count,
            rows.Count(row => row.Movement == PriorDayMovement.New),
            rows.Count(row => row.Movement == PriorDayMovement.Resolved),
            rows.Count(row => row.Movement == PriorDayMovement.Increased),
            rows.Count(row => row.Movement == PriorDayMovement.Decreased),
            rows.Count(row => row.Movement == PriorDayMovement.Unchanged),
            rows.Count(row => row.Movement == PriorDayMovement.StateTransition));
    }
}

public sealed record PriorDayMovers(IReadOnlyList<PriorDayComparisonRow> TopByUsageRatioDelta);

public sealed record PriorDayDataFact(
    string CurrentBaseDate,
    string PriorBaseDate,
    PriorDayKpis Kpis,
    IReadOnlyList<PriorDayComparisonRow> ComparisonTable,
    PriorDayMovers Movers);

public sealed record PriorDayMethodology(
    string PriorBaseDateSelectionRule,
    string JoinKeyRule,
    string MoverRankingRule,
    string DraftNotice);

public sealed record PriorDayUserValidation(IReadOnlyList<string> ChecklistItems);

public sealed record PriorDayHiddenRisk(IReadOnlyList<SafetyFinding> Findings);

public sealed record PriorDayContract(
    PriorDayDataFact DataFact,
    PriorDayMethodology Methodology,
    PriorDayUserValidation UserValidation,
    PriorDayHiddenRisk HiddenRisk);

public sealed record PriorDayAnalysisResult(
    PriorDayContract Contract,
    LimitAnalysisResult Current,
    LimitAnalysisResult Prior,
    bool IsDeterministic);
