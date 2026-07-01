using System.Text;
using System.Text.Json;
using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Feedback;

public sealed class PromotedExampleStore
{
    private static readonly Encoding JsonlEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string filePath;

    public PromotedExampleStore(string configDirectory = "config", string fileName = "promoted_examples.jsonl")
    {
        filePath = ResolveConfigFile(configDirectory, fileName);
    }

    public string FilePath => filePath;

    public void Append(IEnumerable<PromotedExample> promotedExamples)
    {
        ArgumentNullException.ThrowIfNull(promotedExamples);

        var lines = new List<string>();
        foreach (var example in promotedExamples)
        {
            Validate(example);
            lines.Add(JsonSerializer.Serialize(example, JsonOptions));
        }

        if (lines.Count == 0)
        {
            return;
        }

        File.AppendAllLines(filePath, lines, JsonlEncoding);
    }

    public IReadOnlyList<PromotedExample> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<PromotedExample>();
        }

        var examples = new List<PromotedExample>();
        foreach (var line in File.ReadLines(filePath, JsonlEncoding))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var example = JsonSerializer.Deserialize<PromotedExample>(line, JsonOptions);
            if (example is not null)
            {
                examples.Add(example);
            }
        }

        return examples;
    }

    private static void Validate(PromotedExample example)
    {
        if (string.IsNullOrWhiteSpace(example.ExampleId))
        {
            throw new ArgumentException("ExampleId가 비어 있습니다.", nameof(example));
        }

        if (string.IsNullOrWhiteSpace(example.FeedbackId))
        {
            throw new ArgumentException("FeedbackId가 비어 있습니다.", nameof(example));
        }

        if (!LogHash.IsSha256Hex(example.UserIdHash))
        {
            throw new ArgumentException("UserIdHash는 SHA-256 hex 해시여야 합니다.", nameof(example));
        }

        if (example.ExampleBody is null)
        {
            if (example.ExampleBodyLength != 0 || example.ExampleBodyHash is not null)
            {
                throw new ArgumentException("본문이 없는 승격 예제는 길이 0과 null 해시만 허용됩니다.", nameof(example));
            }

            return;
        }

        if (example.ExampleBodyLength != example.ExampleBody.Length)
        {
            throw new ArgumentException("ExampleBodyLength가 본문 길이와 일치하지 않습니다.", nameof(example));
        }

        if (!LogHash.IsSha256Hex(example.ExampleBodyHash ?? string.Empty)
            || example.ExampleBodyHash != LogHash.Sha256Hex(example.ExampleBody))
        {
            throw new ArgumentException("ExampleBodyHash는 ExampleBody의 SHA-256 hex 해시여야 합니다.", nameof(example));
        }
    }

    private static string ResolveConfigFile(string configDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            throw new ArgumentException("config 디렉터리가 비어 있습니다.", nameof(configDirectory));
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("config 파일명이 올바르지 않습니다.", nameof(fileName));
        }

        if (!string.Equals(Path.GetExtension(fileName), ".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("승격 저장소는 jsonl 파일만 허용됩니다.", nameof(fileName));
        }

        if (Path.IsPathRooted(configDirectory) || ContainsParentTraversal(configDirectory))
        {
            throw new ArgumentException("승격 저장소 경로는 repo/app 기준 config 하위 상대경로만 허용됩니다.", nameof(configDirectory));
        }

        var configRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "config"));
        var targetDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configDirectory));
        if (!targetDirectory.Equals(configRoot, StringComparison.OrdinalIgnoreCase)
            && !targetDirectory.StartsWith(configRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("승격 저장소 쓰기 경로는 config 하위만 허용됩니다.", nameof(configDirectory));
        }

        Directory.CreateDirectory(targetDirectory);
        return Path.Combine(targetDirectory, fileName);
    }

    private static bool ContainsParentTraversal(string path)
    {
        var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }
}
