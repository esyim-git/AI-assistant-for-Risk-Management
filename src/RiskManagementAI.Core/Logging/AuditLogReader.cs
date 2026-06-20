using System.Text;
using System.Text.Json;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Logging;

public sealed class AuditLogReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuditLogReadResult Read(string logDirectory = "logs", int maxRows = 200)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows), "최대 조회 건수는 1 이상이어야 합니다.");
        }

        var findings = new List<SafetyFinding>();
        var records = new List<AuditLogRecord>();
        records.AddRange(ReadTaskLog(logDirectory, findings));
        records.AddRange(ReadFeedbackLog(logDirectory, findings));

        var ordered = records
            .OrderByDescending(record => record.CreatedAt)
            .ThenBy(record => record.Source, StringComparer.Ordinal)
            .Take(maxRows)
            .ToList();

        if (ordered.Count == 0 && findings.Count == 0)
        {
            findings.Add(new SafetyFinding("AUDIT_LOG_EMPTY", SafetySeverity.Low, "표시할 감사 로그가 없습니다."));
        }

        return new AuditLogReadResult(ordered, findings);
    }

    private static IEnumerable<AuditLogRecord> ReadTaskLog(string logDirectory, List<SafetyFinding> findings)
    {
        var path = LogPathResolver.ResolveLogFileForRead(logDirectory, "task_log.jsonl");
        foreach (var entry in ReadJsonLines<TaskLogEntry>(path, "TaskLog", findings))
        {
            yield return new AuditLogRecord(
                "TaskLog",
                entry.TaskId,
                entry.TaskId,
                entry.CreatedAt,
                entry.TaskType,
                entry.ToolType,
                entry.SafetyResult,
                entry.RuleVersion,
                HashPrefix(entry.UserId),
                HashPrefix(entry.RequestHash),
                HashPrefix(entry.OutputHash));
        }
    }

    private static IEnumerable<AuditLogRecord> ReadFeedbackLog(string logDirectory, List<SafetyFinding> findings)
    {
        var path = LogPathResolver.ResolveLogFileForRead(logDirectory, "feedback_log.jsonl");
        foreach (var entry in ReadJsonLines<FeedbackLogEntry>(path, "FeedbackLog", findings))
        {
            yield return new AuditLogRecord(
                "FeedbackLog",
                entry.FeedbackId,
                entry.TaskId,
                entry.CreatedAt,
                entry.FeedbackCode,
                "Feedback",
                entry.ReviewStatus,
                string.Empty,
                HashPrefix(entry.UserId),
                string.Empty,
                string.Empty);
        }
    }

    private static IEnumerable<T> ReadJsonLines<T>(string path, string source, List<SafetyFinding> findings)
    {
        if (!File.Exists(path))
        {
            findings.Add(new SafetyFinding("AUDIT_LOG_FILE_MISSING", SafetySeverity.Low, $"{source} 파일이 없습니다. path={Path.GetFileName(path)}"));
            yield break;
        }

        var lineNumber = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T? entry;
            try
            {
                entry = JsonSerializer.Deserialize<T>(line, JsonOptions);
            }
            catch (JsonException)
            {
                findings.Add(new SafetyFinding("AUDIT_LOG_LINE_INVALID", SafetySeverity.Low, $"{source} line {lineNumber} JSON을 읽지 못했습니다."));
                continue;
            }

            if (entry is null)
            {
                findings.Add(new SafetyFinding("AUDIT_LOG_LINE_EMPTY", SafetySeverity.Low, $"{source} line {lineNumber} 항목이 비어 있습니다."));
                continue;
            }

            yield return entry;
        }
    }

    private static string HashPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 12 ? trimmed : trimmed[..12];
    }
}

public sealed record AuditLogReadResult(
    IReadOnlyList<AuditLogRecord> Records,
    IReadOnlyList<SafetyFinding> Findings);

public sealed record AuditLogRecord(
    string Source,
    string EntryId,
    string TaskId,
    DateTime CreatedAt,
    string ActivityType,
    string Detail,
    string Result,
    string RuleVersion,
    string UserHashPrefix,
    string RequestHashPrefix,
    string OutputHashPrefix);
