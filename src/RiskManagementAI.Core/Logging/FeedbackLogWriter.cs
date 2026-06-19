using System.Text;
using System.Text.Json;

namespace RiskManagementAI.Core.Logging;

public sealed class FeedbackLogWriter
{
    private static readonly Encoding JsonlEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string logFilePath;

    public FeedbackLogWriter(string logDirectory = "logs", string fileName = "feedback_log.jsonl")
    {
        logFilePath = LogPathResolver.ResolveLogFile(logDirectory, fileName);
    }

    public string LogFilePath => logFilePath;

    public void Append(FeedbackLogEntry entry)
    {
        Validate(entry);
        var line = JsonSerializer.Serialize(entry, JsonOptions);
        File.AppendAllText(logFilePath, line + Environment.NewLine, JsonlEncoding);
    }

    private static void Validate(FeedbackLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.FeedbackId))
        {
            throw new ArgumentException("FeedbackId가 비어 있습니다.", nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.TaskId))
        {
            throw new ArgumentException("TaskId가 비어 있습니다.", nameof(entry));
        }
    }
}
