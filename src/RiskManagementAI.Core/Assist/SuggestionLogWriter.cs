using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Assist;

public sealed class SuggestionLogWriter
{
    private static readonly Encoding JsonlEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly string logFilePath;

    public SuggestionLogWriter(string logDirectory = "logs", string fileName = "suggestion_log.jsonl")
    {
        logFilePath = LogPathResolver.ResolveLogFile(logDirectory, fileName);
    }

    public string LogFilePath => logFilePath;

    public void Append(SuggestionLogEntry entry)
    {
        Validate(entry);
        var line = JsonSerializer.Serialize(entry, JsonOptions);
        File.AppendAllText(logFilePath, line + Environment.NewLine, JsonlEncoding);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void Validate(SuggestionLogEntry entry)
    {
        if (!LogHash.IsSha256Hex(entry.SuggestionId))
        {
            throw new ArgumentException("SuggestionId must be a SHA-256 hex hash.", nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.ProviderId))
        {
            throw new ArgumentException("ProviderId is required.", nameof(entry));
        }

        if (entry.Kind is CompletionItemKind.SafetyHint or CompletionItemKind.BlockedHint)
        {
            throw new ArgumentException("Non-insertable safety hints cannot be accepted or audited.", nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.Mode))
        {
            throw new ArgumentException("Mode is required.", nameof(entry));
        }

        if (!LogHash.IsSha256Hex(entry.UserHash))
        {
            throw new ArgumentException("UserHash must be a SHA-256 hex hash.", nameof(entry));
        }

        if (!LogHash.IsSha256Hex(entry.InsertTextHash))
        {
            throw new ArgumentException("InsertTextHash must be a SHA-256 hex hash.", nameof(entry));
        }

        if (entry.AcceptedAtUtc == default)
        {
            throw new ArgumentException("AcceptedAtUtc is required.", nameof(entry));
        }
    }
}
