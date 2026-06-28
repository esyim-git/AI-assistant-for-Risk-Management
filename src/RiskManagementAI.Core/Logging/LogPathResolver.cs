namespace RiskManagementAI.Core.Logging;

internal static class LogPathResolver
{
    private const string LogsRootName = "logs";

    public static string ResolveLogFile(string logDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("로그 디렉터리가 비어 있습니다.", nameof(logDirectory));
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("로그 파일명이 올바르지 않습니다.", nameof(fileName));
        }

        if (Path.IsPathRooted(logDirectory) || ContainsParentTraversal(logDirectory))
        {
            throw new ArgumentException("로그 경로는 repo/app 기준 logs 하위 상대경로만 허용됩니다.", nameof(logDirectory));
        }

        var logsRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, LogsRootName));
        var targetDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, logDirectory));
        if (!targetDirectory.Equals(logsRoot, StringComparison.OrdinalIgnoreCase)
            && !targetDirectory.StartsWith(logsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("운영환경 쓰기 경로는 logs 하위만 허용됩니다.", nameof(logDirectory));
        }

        Directory.CreateDirectory(targetDirectory);
        return Path.Combine(targetDirectory, fileName);
    }

    public static string ResolveLogFileForRead(string logDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("로그 디렉터리가 비어 있습니다.", nameof(logDirectory));
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("로그 파일명이 올바르지 않습니다.", nameof(fileName));
        }

        if (Path.IsPathRooted(logDirectory) || ContainsParentTraversal(logDirectory))
        {
            throw new ArgumentException("로그 경로는 repo/app 기준 logs 하위 상대경로만 허용됩니다.", nameof(logDirectory));
        }

        var logsRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, LogsRootName));
        var targetDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, logDirectory));
        if (!targetDirectory.Equals(logsRoot, StringComparison.OrdinalIgnoreCase)
            && !targetDirectory.StartsWith(logsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("운영환경 읽기 경로는 logs 하위만 허용됩니다.", nameof(logDirectory));
        }

        return Path.Combine(targetDirectory, fileName);
    }

    private static bool ContainsParentTraversal(string path)
    {
        var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }
}
