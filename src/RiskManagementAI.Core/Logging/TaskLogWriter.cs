using System.Text;
using System.Text.Json;

namespace RiskManagementAI.Core.Logging;

public sealed class TaskLogWriter
{
    private static readonly Encoding JsonlEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string logFilePath;

    public TaskLogWriter(string logDirectory = "logs", string fileName = "task_log.jsonl")
    {
        logFilePath = LogPathResolver.ResolveLogFile(logDirectory, fileName);
    }

    public string LogFilePath => logFilePath;

    public void Append(TaskLogEntry entry)
    {
        Validate(entry);
        var line = JsonSerializer.Serialize(entry, JsonOptions);
        File.AppendAllText(logFilePath, line + Environment.NewLine, JsonlEncoding);
    }

    private static void Validate(TaskLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.TaskId))
        {
            throw new ArgumentException("TaskId가 비어 있습니다.", nameof(entry));
        }

        if (!LogHash.IsSha256Hex(entry.RequestHash))
        {
            throw new ArgumentException("RequestHash는 SHA-256 hex 해시여야 합니다.", nameof(entry));
        }

        if (entry.OutputHash is not null && !LogHash.IsSha256Hex(entry.OutputHash))
        {
            throw new ArgumentException("OutputHash는 SHA-256 hex 해시여야 합니다.", nameof(entry));
        }
    }
}
