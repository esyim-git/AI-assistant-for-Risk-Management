using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Integrity;

/// <summary>
/// In-process port of <c>build/03_verify-package.ps1</c> §4 (STAB-WP-03a) for runtime fail-closed
/// verification (STAB-WP-03b, ADR-008). Hash-only (SHA256), deterministic, zero external dependency
/// (in-box <see cref="System.Security.Cryptography"/> + <see cref="System.Text.Json"/> only).
///
/// INTERIM SCOPE (Design 3): detects tampering of integrity-critical <em>data/asset</em> files
/// (policy / mapping / rules / templates / KB / NCR) and a missing / shrunken / unparseable manifest.
/// It does NOT establish an independent trust anchor for the manifest itself: an attacker with write
/// access to the extracted folder can edit a file <em>and</em> its manifest entry in lock-step and pass.
/// That residual (plus the unhashed self-contained runtime DLLs) is deferred to Authenticode code
/// signing (APPROVAL_REQUIRED, ADR-008 follow-up). Keep this verifier in lock-step with build/03 §4.
/// </summary>
public static class IntegrityVerifier
{
    /// <summary>
    /// Expected package version. Anchored to a Core build constant (ADR-006), NOT the runtime
    /// Assembly version: a tampered file-version cannot satisfy it and the value is reproducible
    /// offline. Bump in lock-step with the VERSION file at each release.
    /// </summary>
    public const string ExpectedVersion = "0.6.0";

    public const string ManifestFileName = "approved_manifest.json";

    /// <summary>
    /// Mandatory core entries that MUST be declared in the manifest (mirrors build/03 §4a). An empty
    /// or partial manifest must not pass (RR-14). Keep identical to the build/03 mandatory list.
    /// </summary>
    public static readonly IReadOnlyList<string> MandatoryEntries = new[]
    {
        "RiskManagementAI.exe",
        "RiskManagementAI.dll",
        "RiskManagementAI.Core.dll",
        "config/security_policy.json",
        "config/column_mapping.json",
        "kb/ncr_placeholder.md"
    };

    // O(1) lookup so a missing mandatory file fails closed even when its manifest entry was tampered
    // to required:false. Ordinal: manifest paths are exact forward-slash (build/01).
    private static readonly HashSet<string> MandatorySet = new(MandatoryEntries, StringComparer.Ordinal);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Verify the integrity manifest of an extracted package rooted at <paramref name="baseDir"/>.
    /// When <paramref name="strict"/> is true (operational/release), any failure — including an
    /// absent, unreadable, unparseable, or empty manifest — yields <see cref="IntegrityStatus.FailClosed"/>.
    /// When false (explicit dev switch), failures are downgraded to <see cref="IntegrityStatus.DevFallback"/>.
    /// </summary>
    public static IntegrityResult VerifyPackage(string baseDir, bool strict)
    {
        var problems = new List<string>();
        var blockedClasses = new SortedSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(baseDir))
        {
            problems.Add("integrity base directory is empty");
            return Build(strict, problems, blockedClasses, manifestPresent: false);
        }

        // Normalize the package root with a trailing separator (build/03 $rootNorm) so the
        // StartsWith containment check below cannot be satisfied by a sibling-prefix directory.
        string rootNorm;
        try
        {
            rootNorm = Path.GetFullPath(baseDir);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            problems.Add($"integrity base directory is invalid: {ex.Message}");
            return Build(strict, problems, blockedClasses, manifestPresent: false);
        }

        if (!rootNorm.EndsWith(Path.DirectorySeparatorChar))
        {
            rootNorm += Path.DirectorySeparatorChar;
        }

        var manifestFile = Path.Combine(baseDir, ManifestFileName);

        // Absent / unreadable / unparseable / empty manifest => fail-closed (strict) or dev-fallback.
        // A missing manifest is NEVER treated as "development" (RR-14).
        if (!File.Exists(manifestFile))
        {
            problems.Add($"{ManifestFileName} missing from package");
            return Build(strict, problems, blockedClasses, manifestPresent: false);
        }

        ManifestModel? manifest;
        try
        {
            var json = File.ReadAllText(manifestFile);
            manifest = JsonSerializer.Deserialize<ManifestModel>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            problems.Add($"{ManifestFileName} could not be read or parsed: {ex.Message}");
            return Build(strict, problems, blockedClasses, manifestPresent: false);
        }

        if (manifest?.Files is null || manifest.Files.Count == 0)
        {
            problems.Add($"{ManifestFileName} is empty or declares no file entries");
            return Build(strict, problems, blockedClasses, manifestPresent: false);
        }

        // Version is compared to the Core build constant, not the manifest-declared value alone.
        if (!string.Equals(manifest.Version, ExpectedVersion, StringComparison.Ordinal))
        {
            problems.Add($"manifest version '{manifest.Version}' != expected '{ExpectedVersion}'");
            blockedClasses.Add("App");
        }

        // A parseable-but-corrupt manifest may carry null file entries (e.g. {"files":[null]}); the
        // empty-guard above does not catch them (Count > 0). Treat any null entry as tamper so the
        // verifier fails closed here instead of throwing a NullReferenceException downstream.
        if (manifest.Files.Any(f => f is null))
        {
            problems.Add("manifest contains a null file entry");
            blockedClasses.Add("App");
        }

        // 4a) Every mandatory core entry must be declared (independent of per-entry verification).
        var declaredPaths = new HashSet<string>(
            manifest.Files.Where(f => f is not null && !string.IsNullOrEmpty(f.Path)).Select(f => f!.Path!),
            StringComparer.Ordinal);
        foreach (var mandatory in MandatoryEntries)
        {
            if (!declaredPaths.Contains(mandatory))
            {
                problems.Add($"manifest missing mandatory entry: {mandatory}");
                blockedClasses.Add(ClassForPath(mandatory));
            }
        }

        // 4b) Verify each declared entry. Aggregate ALL findings; never early-return Ok.
        foreach (var entry in manifest.Files)
        {
            if (entry is null)
            {
                continue; // recorded above as a problem (corrupt manifest) — never early-return Ok.
            }

            var entryClass = string.IsNullOrWhiteSpace(entry.Class) ? "App" : entry.Class!;
            var entryPath = entry.Path;

            if (string.IsNullOrWhiteSpace(entryPath))
            {
                problems.Add("manifest entry with empty path");
                blockedClasses.Add(entryClass);
                continue;
            }

            // Reject rooted / UNC / traversal paths before combining (manifest path must stay under
            // root). Platform-independent: Path.IsPathRooted does not recognize Windows drive/UNC
            // roots on non-Windows hosts, so they are checked explicitly too.
            if (IsRootedOrTraversal(entryPath))
            {
                problems.Add($"manifest path is rooted or contains traversal: {entryPath}");
                blockedClasses.Add(entryClass);
                continue;
            }

            // A malformed path (embedded NUL, overlong, illegal chars) makes Path.GetFullPath throw;
            // capture it as a verification failure instead of letting it escape the fail-closed path.
            string entryFull;
            try
            {
                entryFull = Path.GetFullPath(Path.Combine(baseDir, entryPath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
            {
                problems.Add($"manifest path is malformed: {entryPath} ({ex.Message})");
                blockedClasses.Add(entryClass);
                continue;
            }

            if (!entryFull.StartsWith(rootNorm, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"manifest path escapes package root: {entryPath}");
                blockedClasses.Add(entryClass);
                continue;
            }

            if (!File.Exists(entryFull))
            {
                // Mandatory core files are required by path regardless of the manifest-controlled
                // `required` flag — a tampered required:false must not suppress a missing-file failure.
                if (entry.Required || MandatorySet.Contains(entryPath))
                {
                    problems.Add($"required file missing: {entryPath}");
                    blockedClasses.Add(entryClass);
                }

                continue;
            }

            string actualHash;
            long actualSize;
            try
            {
                using var stream = File.OpenRead(entryFull);
                actualSize = stream.Length;
                actualHash = Convert.ToHexString(SHA256.HashData(stream));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problems.Add($"could not hash file: {entryPath} ({ex.Message})");
                blockedClasses.Add(entryClass);
                continue;
            }

            if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"hash mismatch [{entryClass}]: {entryPath} " +
                    $"(manifest={IntegrityResult.ShortHash(entry.Sha256)}, actual={IntegrityResult.ShortHash(actualHash)})");
                blockedClasses.Add(entryClass);
            }

            // Size is checked independently (mirrors build/03, which reports both).
            if (actualSize != entry.Size)
            {
                problems.Add($"size mismatch: {entryPath} (manifest={entry.Size}, actual={actualSize})");
                blockedClasses.Add(entryClass);
            }
        }

        return Build(strict, problems, blockedClasses, manifestPresent: true);
    }

    /// <summary>Map a manifest entry/path to its security class for blocking attribution.</summary>
    private static string ClassForPath(string path) => path switch
    {
        "config/security_policy.json" => "Policy",
        "config/column_mapping.json" => "Mapping",
        "kb/ncr_placeholder.md" => "Kb",
        _ => "App"
    };

    /// <summary>
    /// Platform-independent rejection of paths that must never appear in a manifest: traversal,
    /// POSIX/Windows roots, Windows drive-letter roots (<c>X:</c>), and UNC roots (<c>\\</c> or <c>//</c>).
    /// <see cref="Path.IsPathRooted"/> alone misses Windows drive/UNC roots on non-Windows hosts.
    /// </summary>
    private static bool IsRootedOrTraversal(string path)
    {
        if (Path.IsPathRooted(path) || path.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        if (path.StartsWith('/') || path.StartsWith('\\'))
        {
            return true; // POSIX root or UNC/backslash root, independent of host OS.
        }

        // Windows drive-letter root, e.g. "C:" or "c:/...".
        return path.Length >= 2
            && path[1] == ':'
            && ((path[0] >= 'A' && path[0] <= 'Z') || (path[0] >= 'a' && path[0] <= 'z'));
    }

    private static IntegrityResult Build(bool strict, List<string> problems, SortedSet<string> blockedClasses, bool manifestPresent)
    {
        if (problems.Count == 0)
        {
            return new IntegrityResult(
                IntegrityStatus.Ok,
                UsedDevFallback: false,
                Warnings: Array.Empty<string>(),
                Findings: Array.Empty<SafetyFinding>(),
                BlockedClasses: new HashSet<string>(StringComparer.Ordinal));
        }

        if (!strict)
        {
            // Explicit dev switch: downgrade every failure to a warning and proceed.
            var devWarnings = new List<string>
            {
                "DEV: integrity verification bypassed via developer switch. NOT valid for a release package."
            };
            devWarnings.AddRange(problems);

            var devFindings = new List<SafetyFinding>
            {
                new(
                    "INTEGRITY_DEV_FALLBACK",
                    SafetySeverity.Medium,
                    "개발용 무결성 우회가 활성화되어 검증 실패를 경고로 처리했습니다. 운영 패키지에서는 적용되지 않습니다.")
            };

            return new IntegrityResult(
                IntegrityStatus.DevFallback,
                UsedDevFallback: true,
                Warnings: devWarnings,
                Findings: devFindings,
                BlockedClasses: new HashSet<string>(StringComparer.Ordinal));
        }

        // Operational (strict): fail-closed. Emit one HIGH finding per blocked class (audit-friendly).
        var findings = new List<SafetyFinding>();
        if (!manifestPresent || blockedClasses.Count == 0)
        {
            blockedClasses.Add("All");
            findings.Add(new SafetyFinding(
                "INTEGRITY_MANIFEST_UNVERIFIED",
                SafetySeverity.High,
                "승인 매니페스트(approved_manifest.json)를 확인할 수 없어 무결성을 검증하지 못했습니다. 운영 모드에서는 기동을 차단합니다."));
        }
        else
        {
            foreach (var cls in blockedClasses)
            {
                findings.Add(new SafetyFinding(
                    $"INTEGRITY_{cls.ToUpperInvariant()}_TAMPERED",
                    SafetySeverity.High,
                    $"무결성 검증 실패: '{cls}' 클래스 파일이 승인 매니페스트와 일치하지 않습니다. 해당 기능은 차단됩니다."));
            }
        }

        return new IntegrityResult(
            IntegrityStatus.FailClosed,
            UsedDevFallback: false,
            Warnings: problems,
            Findings: findings,
            BlockedClasses: blockedClasses);
    }

    private sealed record ManifestModel(
        [property: JsonPropertyName("version")] string? Version,
        [property: JsonPropertyName("generatedAtUtc")] string? GeneratedAtUtc,
        [property: JsonPropertyName("files")] IReadOnlyList<ManifestEntry?>? Files);

    private sealed record ManifestEntry(
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("sha256")] string? Sha256,
        [property: JsonPropertyName("class")] string? Class,
        [property: JsonPropertyName("required")] bool Required);
}
