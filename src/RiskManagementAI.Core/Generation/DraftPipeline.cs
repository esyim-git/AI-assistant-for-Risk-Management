using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Generation;

public sealed record DraftPipelineRequest(
    DraftRequestKind Kind,
    string Prompt,
    string UserId,
    string? Context = null);

public sealed record DraftPipelineResult(
    bool IsAcceptedForReview,
    string SafetyResult,
    string Message,
    string? DraftText,
    IReadOnlyList<SafetyFinding> Findings,
    string TaskId,
    bool AuditLogWritten);

public sealed class DraftPipeline
{
    private readonly ILocalDraftService draftService;
    private readonly SqlSafetyChecker sqlChecker;
    private readonly VbaSafetyChecker vbaChecker;
    private readonly TaskLogWriter taskLogWriter;
    private readonly string ruleVersion;

    public DraftPipeline(
        ILocalDraftService draftService,
        SafetyRuleSet ruleSet,
        TaskLogWriter? taskLogWriter = null)
    {
        this.draftService = draftService;
        sqlChecker = new SqlSafetyChecker(ruleSet);
        vbaChecker = new VbaSafetyChecker(ruleSet);
        this.taskLogWriter = taskLogWriter ?? new TaskLogWriter();
        ruleVersion = ruleSet.RuleVersion;
    }

    public DraftPipelineResult Generate(DraftPipelineRequest request)
    {
        var draftResponse = draftService.GenerateDraft(new DraftRequest(
            request.Kind,
            request.Prompt,
            request.Context));
        var findings = draftResponse.Findings.ToList();

        if (!string.IsNullOrWhiteSpace(draftResponse.DraftText))
        {
            findings.AddRange(CheckDraftSafety(request.Kind, draftResponse.DraftText));
        }
        else if (!draftResponse.IsAvailable)
        {
            findings.Add(new SafetyFinding(
                "DRAFT_NOT_AVAILABLE",
                SafetySeverity.Info,
                "생성 초안이 없어 안전 검사 대상 텍스트가 없습니다."));
        }

        var safetyResult = DetermineSafetyResult(draftResponse, findings);
        var isAcceptedForReview = safetyResult == "PASS" && !string.IsNullOrWhiteSpace(draftResponse.DraftText);
        var taskId = $"task-{Guid.NewGuid():N}";
        var auditLogWritten = TryAppendAuditLog(taskId, request, draftResponse, safetyResult, findings);

        if (!auditLogWritten)
        {
            safetyResult = DetermineSafetyResult(draftResponse, findings);
            isAcceptedForReview = false;
        }

        return new DraftPipelineResult(
            isAcceptedForReview,
            safetyResult,
            draftResponse.Message,
            isAcceptedForReview ? draftResponse.DraftText : null,
            findings,
            taskId,
            auditLogWritten);
    }

    private IEnumerable<SafetyFinding> CheckDraftSafety(DraftRequestKind kind, string draftText)
    {
        return kind switch
        {
            DraftRequestKind.Sql => sqlChecker.Check(draftText),
            DraftRequestKind.Vba => vbaChecker.Check(draftText),
            _ => new[]
            {
                new SafetyFinding(
                    "DRAFT_SAFETY_NOT_APPLICABLE",
                    SafetySeverity.Info,
                    "이 초안 유형에는 SQL/VBA 안전 검사가 적용되지 않습니다.")
            }
        };
    }

    private bool TryAppendAuditLog(
        string taskId,
        DraftPipelineRequest request,
        DraftResponse draftResponse,
        string safetyResult,
        List<SafetyFinding> findings)
    {
        try
        {
            var outputMaterial = draftResponse.DraftText ?? draftResponse.Message;
            taskLogWriter.Append(new TaskLogEntry(
                taskId,
                DateTime.UtcNow,
                LogHash.Sha256Hex(string.IsNullOrWhiteSpace(request.UserId) ? "anonymous" : request.UserId),
                TaskTypeFor(request.Kind),
                nameof(DraftPipeline),
                LogHash.Sha256Hex($"{request.Kind}|{request.Prompt}|{request.Context ?? string.Empty}"),
                LogHash.Sha256Hex(outputMaterial),
                safetyResult,
                ruleVersion));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            findings.Add(new SafetyFinding("TASK_LOG_WRITE_FAILED", SafetySeverity.High, ex.Message));
            return false;
        }
    }

    private static string DetermineSafetyResult(DraftResponse draftResponse, IReadOnlyList<SafetyFinding> findings)
    {
        if (findings.Any(f => f.Severity == SafetySeverity.Blocker))
        {
            return "BLOCKED";
        }

        if (findings.Any(f => f.Severity == SafetySeverity.High))
        {
            return "REVIEW_REQUIRED";
        }

        if (!draftResponse.IsAvailable && string.IsNullOrWhiteSpace(draftResponse.DraftText))
        {
            return "NO_MODEL";
        }

        return "PASS";
    }

    private static string TaskTypeFor(DraftRequestKind kind)
    {
        return kind switch
        {
            DraftRequestKind.Sql => "SqlDraftGeneration",
            DraftRequestKind.Vba => "VbaDraftGeneration",
            DraftRequestKind.Regulation => "RegulationDraftGeneration",
            _ => "GeneralDraftGeneration"
        };
    }
}
