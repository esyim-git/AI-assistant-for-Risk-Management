internal static class GenerationTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
var policyLoadResult = PolicyLoader.LoadDefault();
var noModelDraftService = new NoModelDraftService(policyLoadResult.Policy);
var noModelDraftResponse = noModelDraftService.GenerateDraft(new DraftRequest(
    DraftRequestKind.Sql,
    "SELECT draft request for review-only output",
    "dummy context"));
context.AssertTrue(!noModelDraftResponse.IsAvailable, "NoModelDraftService should keep generation unavailable");
context.AssertTrue(noModelDraftResponse.Mode == NoModelDraftService.ModeName, "NoModelDraftService should report NoModelMode");
context.AssertTrue(noModelDraftResponse.DraftText is null, "NoModelDraftService should not return generated draft text");
context.AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_NO_MODEL_MODE" && f.Severity == SafetySeverity.Info), "NoModelDraftService should return safe no-model guidance");
context.AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_EXTERNAL_COMM_BLOCKED"), "NoModelDraftService should confirm external communications are blocked");
context.AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_AUTO_EXECUTE_BLOCKED"), "NoModelDraftService should confirm auto execution is blocked");
context.AssertTrue(noModelDraftService.GenerateDraft(null).Findings.Any(f => f.Code == "DRAFT_PROMPT_EMPTY"), "NoModelDraftService should not throw for an empty request");
var noModelNullRequestResponse = noModelDraftService.GenerateDraft(null);
context.AssertTrue(!noModelNullRequestResponse.IsAvailable && noModelNullRequestResponse.DraftText is null && noModelNullRequestResponse.Mode == NoModelDraftService.ModeName, "NoModelDraftService null request should remain unavailable with no draft text");

var unsafeDraftPolicy = policyLoadResult.Policy with
{
    Network = policyLoadResult.Policy.Network with
    {
        AllowExternalApi = true
    }
};
var unsafeDraftResponse = new NoModelDraftService(unsafeDraftPolicy).GenerateDraft(new DraftRequest(DraftRequestKind.General, "policy check"));
context.AssertTrue(unsafeDraftResponse.Findings.Any(f => f.Code == "DRAFT_POLICY_UNSAFE_NETWORK" && f.Severity == SafetySeverity.High), "NoModelDraftService should flag unsafe network policy");

var draftPipelineLogPath = Path.Combine("logs", "smoke_draft_pipeline_log.jsonl");
if (File.Exists(draftPipelineLogPath))
{
    File.Delete(draftPipelineLogPath);
}

var safeSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var safeSqlPipelineResult = safeSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make select draft",
    "user-smoke"));
context.AssertTrue(safeSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should accept safe SQL draft for review");
context.AssertTrue(safeSqlPipelineResult.SafetyResult == "PASS", "DraftPipeline safe SQL result should pass");
context.AssertTrue(safeSqlPipelineResult.AuditLogWritten, "DraftPipeline should audit safe SQL result");
context.AssertTrue(safeSqlPipelineResult.DraftText is not null && safeSqlPipelineResult.DraftText.Contains("SELECT", StringComparison.OrdinalIgnoreCase), "DraftPipeline should return accepted safe SQL draft");

var blockedSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "DELETE FROM TRADE_SAMPLE",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var blockedSqlPipelineResult = blockedSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make delete draft",
    "user-smoke"));
context.AssertTrue(!blockedSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should reject blocker SQL draft");
context.AssertTrue(blockedSqlPipelineResult.SafetyResult == "BLOCKED", "DraftPipeline blocker SQL result should be blocked");
context.AssertTrue(blockedSqlPipelineResult.DraftText is null, "DraftPipeline should suppress blocked draft text");
context.AssertTrue(blockedSqlPipelineResult.Findings.Any(f => f.Code == "SQL_DML_DELETE"), "DraftPipeline should include SQL checker blocker finding");

var reviewRequiredPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "review required",
        "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT",
        [new SafetyFinding("DRAFT_REVIEW_REQUIRED_SMOKE", SafetySeverity.High, "Synthetic review gate.")])),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var reviewRequiredPipelineResult = reviewRequiredPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make review-required draft",
    "user-smoke"));
context.AssertTrue(!reviewRequiredPipelineResult.IsAcceptedForReview && reviewRequiredPipelineResult.SafetyResult == "REVIEW_REQUIRED", "DraftPipeline high finding draft should require review");
context.AssertTrue(reviewRequiredPipelineResult.DraftText is null && reviewRequiredPipelineResult.AuditLogWritten, "DraftPipeline review-required draft should suppress text and keep logged outcome");

var noModelPipeline = new DraftPipeline(noModelDraftService, loadedRuleSet, new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var noModelPipelineResult = noModelPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Vba,
    "make vba draft",
    "user-smoke"));
context.AssertTrue(!noModelPipelineResult.IsAcceptedForReview, "DraftPipeline should keep NoModel result unavailable");
context.AssertTrue(noModelPipelineResult.SafetyResult == "NO_MODEL", "DraftPipeline NoModel result should report NO_MODEL");
context.AssertTrue(noModelPipelineResult.AuditLogWritten, "DraftPipeline should audit NoModel result");

var draftPipelineLogText = File.ReadAllText(draftPipelineLogPath);
context.AssertTrue(!draftPipelineLogText.Contains("make select draft", StringComparison.Ordinal), "DraftPipeline audit should not store raw prompt text");
context.AssertTrue(!draftPipelineLogText.Contains("SELECT TRADE_ID", StringComparison.Ordinal), "DraftPipeline audit should not store raw draft text");
context.AssertTrue(!draftPipelineLogText.Contains("user-smoke", StringComparison.Ordinal), "DraftPipeline audit should not store raw user id");
    }
}
