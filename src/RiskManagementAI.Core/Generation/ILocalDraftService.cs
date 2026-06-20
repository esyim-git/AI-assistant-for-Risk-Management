using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Generation;

public enum DraftRequestKind
{
    General,
    Sql,
    Vba,
    Regulation
}

public sealed record DraftRequest(
    DraftRequestKind Kind,
    string Prompt,
    string? Context = null);

public sealed record DraftResponse(
    bool IsAvailable,
    string Mode,
    string Message,
    string? DraftText,
    IReadOnlyList<SafetyFinding> Findings);

public interface ILocalDraftService
{
    DraftResponse GenerateDraft(DraftRequest? request);
}
