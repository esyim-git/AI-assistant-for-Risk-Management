internal static class AuditTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
var policyLoadResult = PolicyLoader.LoadDefault();
var regulationCatalog = RegulationCatalog.LoadDefault();
var approvedFeedback = new FeedbackLogEntry(
    "feedback-approved-001",
    "task-smoke-approved-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("reviewer-one"),
    "APPROVED",
    "ReviewerApproved");
var rejectedFeedback = approvedFeedback with
{
    FeedbackId = "feedback-rejected-001",
    TaskId = "task-smoke-rejected-001",
    FeedbackCode = "REJECTED",
    ReviewStatus = "ReviewerRejected"
};
var pendingFeedback = approvedFeedback with
{
    FeedbackId = "feedback-pending-001",
    TaskId = "task-smoke-pending-001",
    FeedbackCode = "PENDING",
    ReviewStatus = "ReviewerPending"
};
var promotionResult = new ExamplePromotion().PromoteApproved(
    [approvedFeedback, rejectedFeedback, pendingFeedback, approvedFeedback],
    new DateTime(2026, 06, 19, 0, 0, 0, DateTimeKind.Utc));
context.AssertTrue(promotionResult.PromotedExamples.Count == 1, "ExamplePromotion should promote only approved feedback once");
context.AssertTrue(promotionResult.PromotedExamples[0].PromotionMode == ExamplePromotion.PromotionModeName, "ExamplePromotion should use curation-only mode");
context.AssertTrue(promotionResult.PromotedExamples[0].UserIdHash == approvedFeedback.UserId, "ExamplePromotion should preserve hashed user id");
context.AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == rejectedFeedback.FeedbackId), "ExamplePromotion should skip rejected feedback");
context.AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == pendingFeedback.FeedbackId), "ExamplePromotion should skip pending feedback");
context.AssertTrue(promotionResult.Warnings.Any(warning => warning.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)), "ExamplePromotion should warn on duplicate approved feedback");

var promotedExampleStorePath = Path.Combine("config", "smoke_promoted_examples.jsonl");
if (File.Exists(promotedExampleStorePath))
{
    File.Delete(promotedExampleStorePath);
}

var promotedExampleStore = new PromotedExampleStore("config", "smoke_promoted_examples.jsonl");
promotedExampleStore.Append(promotionResult.PromotedExamples);
promotedExampleStore.Append(promotionResult.PromotedExamples);
var storedPromotedExamples = promotedExampleStore.ReadAll();
var promotedExampleStoreText = File.ReadAllText(promotedExampleStore.FilePath);
context.AssertTrue(storedPromotedExamples.Count == 2, "PromotedExampleStore should append promoted examples");
context.AssertTrue(storedPromotedExamples.All(example => example.UserIdHash == approvedFeedback.UserId), "PromotedExampleStore should persist hashed user ids");
context.AssertTrue(!promotedExampleStoreText.Contains("user-smoke", StringComparison.Ordinal), "PromotedExampleStore should not store raw user id");
context.AssertTrue(context.Throws<ArgumentException>(() => promotedExampleStore.Append([promotionResult.PromotedExamples[0] with { UserIdHash = "plain-user" }])), "PromotedExampleStore should reject non-hash UserIdHash");
context.AssertTrue(context.Throws<ArgumentException>(() => new PromotedExampleStore("config/../logs", "bad.jsonl")), "PromotedExampleStore should reject paths outside config");
File.Delete(promotedExampleStore.FilePath);

var uiIntegrationLogPath = Path.Combine("logs", "smoke_ui_integration_log.jsonl");
if (File.Exists(uiIntegrationLogPath))
{
    File.Delete(uiIntegrationLogPath);
}

var uiIntegrationLogWriter = new TaskLogWriter("logs", "smoke_ui_integration_log.jsonl");
var uiDraftPipeline = new DraftPipeline(new NoModelDraftService(policyLoadResult.Policy), loadedRuleSet, uiIntegrationLogWriter);
var uiDraftResult = uiDraftPipeline.Generate(new DraftPipelineRequest(DraftRequestKind.Sql, "ui draft smoke", "user-smoke"));
var uiKbSearch = new KbSearch(regulationCatalog, uiIntegrationLogWriter, loadedRuleSet.RuleVersion);
var uiKbResponse = uiKbSearch.Search("NCR", "user-smoke");
var uiPromotionResult = new ExamplePromotion().PromoteApproved([approvedFeedback]);
context.AssertTrue(uiDraftResult.SafetyResult == "NO_MODEL" && uiDraftResult.AuditLogWritten, "UI integration smoke should run NoModel draft pipeline with audit");
context.AssertTrue(uiKbResponse.Results.Count > 0 && uiKbResponse.AuditLogWritten, "UI integration smoke should run catalog search with audit");
context.AssertTrue(uiPromotionResult.PromotedExamples.Count == 1, "UI integration smoke should run feedback promotion");
var uiIntegrationLogText = File.ReadAllText(uiIntegrationLogPath);
context.AssertTrue(!uiIntegrationLogText.Contains("ui draft smoke", StringComparison.Ordinal) && !uiIntegrationLogText.Contains("user-smoke", StringComparison.Ordinal), "UI integration audit should not store raw prompt or user id");
var taskLogPath = Path.Combine("logs", "smoke_task_log.jsonl");
var feedbackLogPath = Path.Combine("logs", "smoke_feedback_log.jsonl");
if (File.Exists(taskLogPath))
{
    File.Delete(taskLogPath);
}

if (File.Exists(feedbackLogPath))
{
    File.Delete(feedbackLogPath);
}

var rawRequest = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = 'DUMMY-001'";
var rawOutput = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = :ACCOUNT_NO";
var taskEntry = new TaskLogEntry(
    "task-smoke-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("user-smoke"),
    "SqlSafetyCheck",
    "SqlSafetyChecker",
    LogHash.Sha256Hex(rawRequest),
    LogHash.Sha256Hex(rawOutput),
    "PASS",
    loadedRuleSet.RuleVersion);

var taskLogWriter = new TaskLogWriter("logs", "smoke_task_log.jsonl");
taskLogWriter.Append(taskEntry);
var taskLogText = File.ReadAllText(taskLogWriter.LogFilePath);
context.AssertTrue(File.Exists(taskLogWriter.LogFilePath), "TaskLogWriter should create JSONL file");
context.AssertTrue(taskLogText.Contains(taskEntry.RequestHash, StringComparison.Ordinal), "TaskLogWriter should store request hash");
context.AssertTrue(!taskLogText.Contains(rawRequest, StringComparison.Ordinal) && !taskLogText.Contains("ACCOUNT_NO", StringComparison.Ordinal), "TaskLogWriter should not store raw request/output text");
context.AssertTrue(context.Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RequestHash = rawRequest })), "TaskLogWriter should reject non-hash RequestHash");
context.AssertTrue(context.Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { OutputHash = rawOutput })), "TaskLogWriter should reject non-hash OutputHash");
context.AssertTrue(context.Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { UserId = "plain-user" })), "TaskLogWriter should reject non-hash UserId");
context.AssertTrue(context.Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RuleVersion = "ruleset-not-valid" })), "TaskLogWriter should reject malformed RuleVersion");
context.AssertTrue(context.Throws<ArgumentException>(() => new TaskLogWriter("logs/../reports", "bad.jsonl")), "TaskLogWriter should reject paths outside logs");

var feedbackEntry = new FeedbackLogEntry(
    "feedback-smoke-001",
    taskEntry.TaskId,
    DateTime.UtcNow,
    taskEntry.UserId,
    "APPROVED",
    "ReviewerApproved");

var feedbackLogWriter = new FeedbackLogWriter("logs", "smoke_feedback_log.jsonl");
feedbackLogWriter.Append(feedbackEntry);
var feedbackLogText = File.ReadAllText(feedbackLogWriter.LogFilePath);
context.AssertTrue(File.Exists(feedbackLogWriter.LogFilePath), "FeedbackLogWriter should create JSONL file");
context.AssertTrue(feedbackLogText.Contains(feedbackEntry.FeedbackId, StringComparison.Ordinal), "FeedbackLogWriter should store feedback id");
context.AssertTrue(!feedbackLogText.Contains(rawRequest, StringComparison.Ordinal), "FeedbackLogWriter should not store raw request text");
context.AssertTrue(context.Throws<ArgumentException>(() => feedbackLogWriter.Append(feedbackEntry with { UserId = "plain-user" })), "FeedbackLogWriter should reject non-hash UserId");

var historyLogDirectory = Path.Combine("logs", "smoke_history_reader");
if (Directory.Exists(historyLogDirectory))
{
    Directory.Delete(historyLogDirectory, recursive: true);
}

var historyTaskWriter = new TaskLogWriter(historyLogDirectory, "task_log.jsonl");
historyTaskWriter.Append(taskEntry);
File.AppendAllText(historyTaskWriter.LogFilePath, "{ broken json" + Environment.NewLine);
var historyFeedbackWriter = new FeedbackLogWriter(historyLogDirectory, "feedback_log.jsonl");
historyFeedbackWriter.Append(feedbackEntry);
var auditLogReader = new AuditLogReader();
var auditReadResult = auditLogReader.Read(historyLogDirectory, maxRows: 10);
context.AssertTrue(auditReadResult.Records.Count == 2, "AuditLogReader should read TaskLog and FeedbackLog records");
context.AssertTrue(auditReadResult.Findings.Any(f => f.Code == "AUDIT_LOG_LINE_INVALID"), "AuditLogReader should warn on invalid JSONL lines");
var taskAuditRecord = auditReadResult.Records.Single(record => record.Source == "TaskLog");
context.AssertTrue(taskAuditRecord.EntryId == taskEntry.TaskId && taskAuditRecord.ActivityType == taskEntry.TaskType, "AuditLogReader should project TaskLog schema");
context.AssertTrue(taskAuditRecord.RequestHashPrefix.Length == 12 && taskAuditRecord.RequestHashPrefix != taskEntry.RequestHash, "AuditLogReader should expose only request hash prefix");
var feedbackAuditRecord = auditReadResult.Records.Single(record => record.Source == "FeedbackLog");
context.AssertTrue(feedbackAuditRecord.EntryId == feedbackEntry.FeedbackId && feedbackAuditRecord.Result == feedbackEntry.ReviewStatus, "AuditLogReader should project FeedbackLog schema");
context.AssertTrue(auditReadResult.Records.All(record => record.UserHashPrefix.Length == 12 && record.UserHashPrefix != taskEntry.UserId), "AuditLogReader should expose only user hash prefixes");
var missingAuditResult = auditLogReader.Read(Path.Combine("logs", "smoke_history_missing"), maxRows: 10);
context.AssertTrue(missingAuditResult.Records.Count == 0 && missingAuditResult.Findings.Any(f => f.Code == "AUDIT_LOG_FILE_MISSING"), "AuditLogReader should gracefully report missing log files");
context.AssertTrue(context.Throws<ArgumentException>(() => auditLogReader.Read("logs/../reports")), "AuditLogReader should reject paths outside logs");
    }
}