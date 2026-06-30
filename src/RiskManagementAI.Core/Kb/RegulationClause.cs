namespace RiskManagementAI.Core.Kb;

public sealed record RegulationClause(
    string ChunkId,
    string SourceId,
    string ClauseRef,
    string ClauseText,
    string EffectiveDate,
    string RepealDate,
    string PackVersion,
    string SourceTextHash);
