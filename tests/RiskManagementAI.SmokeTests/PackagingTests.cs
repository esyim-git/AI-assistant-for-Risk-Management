internal static class PackagingTests
{
    internal static void Run(SmokeTestContext context)
    {
        // STAB-WP-01: VERSION is the single source of truth; build scripts must read it and fail on mismatch (RR-11, ADR-006).
foreach (var buildScript in new[] { "01_publish-win-x64.ps1", "02_package-release.ps1", "03_verify-package.ps1" })
{
    var scriptText = File.ReadAllText(Path.Combine("build", buildScript));
    context.AssertTrue(!scriptText.Contains("0.2.0", StringComparison.Ordinal), $"build/{buildScript} should not hardcode default version 0.2.0 (VERSION is single source)");
    context.AssertTrue(scriptText.Contains("VERSION file", StringComparison.Ordinal), $"build/{buildScript} should resolve version from the VERSION file");
    context.AssertTrue(scriptText.Contains("does not match VERSION file", StringComparison.Ordinal), $"build/{buildScript} should fail when -Version mismatches the VERSION file");
}
context.AssertTrue(!File.ReadAllText(Path.Combine("build", "01_publish-win-x64.ps1")).Contains("signed assembly", StringComparison.OrdinalIgnoreCase), "build/01 manifest logging should not overstate signed-assembly trust anchor before STAB-WP-05");
var build01TextForLocalConfig = File.ReadAllText(Path.Combine("build", "01_publish-win-x64.ps1"));
var build03TextForLocalConfig = File.ReadAllText(Path.Combine("build", "03_verify-package.ps1"));
context.AssertTrue(build01TextForLocalConfig.Contains("*.local.json", StringComparison.Ordinal) && build01TextForLocalConfig.Contains("Remove-Item", StringComparison.Ordinal), "build/01 packaging should exclude local layout json from publish output");
context.AssertTrue(build03TextForLocalConfig.Contains("*.local.json", StringComparison.Ordinal) && build03TextForLocalConfig.Contains("Local runtime config present in package", StringComparison.Ordinal), "build/03 packaging should fail if local layout json is present in release package");
context.AssertTrue(System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText("VERSION").Trim(), @"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"), "VERSION file should be a non-empty semver string (single source of truth; no hardcoded value in tests)");
context.AssertTrue(File.Exists("global.json") && File.ReadAllText("global.json").Contains("8.0", StringComparison.Ordinal), "global.json should pin the .NET 8 SDK band (ADR-005/006)");
// === STAB-WP-03b: runtime fail-closed integrity gate (manifest). Domain keyword "manifest" => Packaging. ===
{
    var integritySpecs = new (string Path, string Class, bool Required)[]
    {
        ("RiskManagementAI.exe", "App", true),
        ("RiskManagementAI.dll", "App", true),
        ("RiskManagementAI.Core.dll", "App", true),
        ("config/security_policy.json", "Policy", true),
        ("config/column_mapping.json", "Mapping", true),
        ("config/ncr/ncr_ruleset_sample.json", "Ncr", true),
        ("kb/README.md", "Kb", true),
        ("kb/ncr_placeholder.md", "Kb", true),
        ("kb/public_regulation_catalog.csv", "Kb", true),
        ("rules/excel_2021_blocked_functions.txt", "Rules", true),
        ("rules/excel_2021_completion_allow_functions.txt", "Rules", true),
        ("rules/excel_2021_preferred_functions.txt", "Rules", true),
        ("rules/sql_deny_patterns.txt", "Rules", true),
        ("rules/sql_warn_patterns.txt", "Rules", true),
        ("rules/vba_deny_patterns.txt", "Rules", true),
        ("rules/vba_warn_patterns.txt", "Rules", true),
        ("templates/report/app.xml.tpl", "Template", true),
        ("templates/report/content_types.xml.tpl", "Template", true),
        ("templates/report/core.xml.tpl", "Template", true),
        ("templates/report/root_rels.xml.tpl", "Template", true),
        ("templates/report/styles.xml.tpl", "Template", true),
        ("templates/report/workbook.xml.tpl", "Template", true),
        ("templates/report/workbook_rels.xml.tpl", "Template", true),
        ("templates/report/worksheet.xml.tpl", "Template", true),
        ("templates/sql/sql_generation_prompt.md", "Template", true),
        ("templates/vba/vba_generation_prompt.md", "Template", true)
    };

    var integrityTempDirs = new List<string>();

    string IntegrityHash(string path)
    {
        using var hashStream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(hashStream));
    }

    string FreshIntegrityPackage()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rmai_integrity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        integrityTempDirs.Add(dir);
        foreach (var spec in integritySpecs)
        {
            var full = Path.Combine(dir, spec.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, $"deterministic-content-of-{spec.Path}");
        }

        return dir;
    }

    List<Dictionary<string, object?>> IntegrityEntries(string dir)
    {
        var entries = new List<Dictionary<string, object?>>();
        foreach (var spec in integritySpecs)
        {
            var full = Path.Combine(dir, spec.Path.Replace('/', Path.DirectorySeparatorChar));
            entries.Add(new Dictionary<string, object?>
            {
                ["path"] = spec.Path,
                ["size"] = new FileInfo(full).Length,
                ["sha256"] = IntegrityHash(full),
                ["class"] = spec.Class,
                ["required"] = spec.Required
            });
        }

        return entries;
    }

    void WriteIntegrityManifest(string dir, string version, List<Dictionary<string, object?>> entries)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["version"] = version,
            ["generatedAtUtc"] = "2026-06-28T00:00:00Z",
            ["files"] = entries
        };
        File.WriteAllText(
            Path.Combine(dir, "approved_manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(manifest));
    }

    // Happy path: clean manifest verifies Ok and the gate allows startup.
    var pkgOk = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgOk, "0.7.0", IntegrityEntries(pkgOk));
    var okResult = IntegrityVerifier.VerifyPackage(pkgOk, strict: true);
    context.AssertTrue(okResult.Status == IntegrityStatus.Ok && okResult.BlockedClasses.Count == 0, "IntegrityVerifier clean manifest verifies Ok in strict mode");
    context.AssertTrue(IntegrityGate.Decide(okResult, devAllow: false) == GateDecision.Allow, "IntegrityGate allows a clean manifest package in release mode");

    // Per-class data tamper: Policy.
    var pkgPolicy = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgPolicy, "0.7.0", IntegrityEntries(pkgPolicy));
    File.WriteAllText(Path.Combine(pkgPolicy, "config", "security_policy.json"), "tampered-policy-content");
    var policyResult = IntegrityVerifier.VerifyPackage(pkgPolicy, strict: true);
    context.AssertTrue(policyResult.Status == IntegrityStatus.FailClosed && policyResult.BlockedClasses.Contains("Policy"), "IntegrityVerifier manifest policy tamper fails closed and blocks the Policy class");
    context.AssertTrue(policyResult.Findings.Any(f => f.Code == "INTEGRITY_POLICY_TAMPERED" && f.Severity == SafetySeverity.High), "IntegrityVerifier manifest policy tamper emits a HIGH Policy finding");
    context.AssertTrue(IntegrityGate.Decide(policyResult, devAllow: false) == GateDecision.Block, "IntegrityGate blocks a tampered policy manifest package");

    // Per-class data tamper: Rules.
    var pkgRules = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgRules, "0.7.0", IntegrityEntries(pkgRules));
    File.WriteAllText(Path.Combine(pkgRules, "rules", "sql_deny_patterns.txt"), "tampered-rules-content");
    var rulesResult = IntegrityVerifier.VerifyPackage(pkgRules, strict: true);
    context.AssertTrue(rulesResult.Status == IntegrityStatus.FailClosed && rulesResult.BlockedClasses.Contains("Rules"), "IntegrityVerifier manifest rules tamper fails closed and blocks the Rules class");

    // Manifest-entry tamper: stored hash altered while the file is unchanged.
    var pkgEntry = FreshIntegrityPackage();
    var entriesEntry = IntegrityEntries(pkgEntry);
    entriesEntry.First(x => (string)x["path"]! == "kb/ncr_placeholder.md")["sha256"] = "00";
    WriteIntegrityManifest(pkgEntry, "0.7.0", entriesEntry);
    var entryResult = IntegrityVerifier.VerifyPackage(pkgEntry, strict: true);
    context.AssertTrue(entryResult.Status == IntegrityStatus.FailClosed && entryResult.BlockedClasses.Contains("Kb"), "IntegrityVerifier manifest entry hash tamper fails closed");

    // Required file missing on disk (manifest entry retained).
    var pkgMissingFile = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgMissingFile, "0.7.0", IntegrityEntries(pkgMissingFile));
    File.Delete(Path.Combine(pkgMissingFile, "kb", "ncr_placeholder.md"));
    var missingFileResult = IntegrityVerifier.VerifyPackage(pkgMissingFile, strict: true);
    context.AssertTrue(missingFileResult.Status == IntegrityStatus.FailClosed && missingFileResult.BlockedClasses.Contains("Kb"), "IntegrityVerifier manifest required file missing fails closed");

    // Mandatory-set shrink: dropping ANY mandatory entry from the manifest must fail closed.
    var mandatoryEntries = new[]
    {
        "RiskManagementAI.exe", "RiskManagementAI.dll", "RiskManagementAI.Core.dll",
        "config/security_policy.json", "config/column_mapping.json", "kb/ncr_placeholder.md"
    };
    foreach (var mandatory in mandatoryEntries)
    {
        var pkgShrink = FreshIntegrityPackage();
        var shrunk = IntegrityEntries(pkgShrink).Where(x => (string)x["path"]! != mandatory).ToList();
        WriteIntegrityManifest(pkgShrink, "0.7.0", shrunk);
        var shrinkResult = IntegrityVerifier.VerifyPackage(pkgShrink, strict: true);
        context.AssertTrue(shrinkResult.Status == IntegrityStatus.FailClosed, $"IntegrityVerifier manifest missing mandatory entry '{mandatory}' fails closed");
    }

    // Path traversal entry.
    var pkgTraversal = FreshIntegrityPackage();
    var traversalEntries = IntegrityEntries(pkgTraversal);
    traversalEntries.Add(new Dictionary<string, object?> { ["path"] = "../evil.txt", ["size"] = 1L, ["sha256"] = "00", ["class"] = "App", ["required"] = false });
    WriteIntegrityManifest(pkgTraversal, "0.7.0", traversalEntries);
    var traversalResult = IntegrityVerifier.VerifyPackage(pkgTraversal, strict: true);
    context.AssertTrue(traversalResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest path traversal entry fails closed");

    // Rooted path entry (OS-appropriate).
    var pkgRooted = FreshIntegrityPackage();
    var rootedEntries = IntegrityEntries(pkgRooted);
    var rootedPath = OperatingSystem.IsWindows() ? "C:/Windows/System32/evil.dll" : "/etc/evil";
    rootedEntries.Add(new Dictionary<string, object?> { ["path"] = rootedPath, ["size"] = 1L, ["sha256"] = "00", ["class"] = "App", ["required"] = false });
    WriteIntegrityManifest(pkgRooted, "0.7.0", rootedEntries);
    var rootedResult = IntegrityVerifier.VerifyPackage(pkgRooted, strict: true);
    context.AssertTrue(rootedResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest rooted path entry fails closed");

    // Size mismatch (correct hash, wrong declared size).
    var pkgSize = FreshIntegrityPackage();
    var sizeEntries = IntegrityEntries(pkgSize);
    sizeEntries.First(x => (string)x["path"]! == "config/column_mapping.json")["size"] = 999999L;
    WriteIntegrityManifest(pkgSize, "0.7.0", sizeEntries);
    var sizeResult = IntegrityVerifier.VerifyPackage(pkgSize, strict: true);
    context.AssertTrue(sizeResult.Status == IntegrityStatus.FailClosed && sizeResult.BlockedClasses.Contains("Mapping"), "IntegrityVerifier manifest size mismatch fails closed");

    // Missing manifest: strict fail-closed; dev switch fallback only.
    var pkgNoManifest = FreshIntegrityPackage();
    var noManifestStrict = IntegrityVerifier.VerifyPackage(pkgNoManifest, strict: true);
    context.AssertTrue(noManifestStrict.Status == IntegrityStatus.FailClosed, "IntegrityVerifier missing manifest fails closed in strict mode");
    context.AssertTrue(IntegrityGate.Decide(noManifestStrict, devAllow: false) == GateDecision.Block, "IntegrityGate blocks a package with no manifest in release mode");
    var noManifestDev = IntegrityVerifier.VerifyPackage(pkgNoManifest, strict: false);
    context.AssertTrue(noManifestDev.Status == IntegrityStatus.DevFallback && noManifestDev.UsedDevFallback, "IntegrityVerifier missing manifest yields dev fallback under the dev switch");
    context.AssertTrue(IntegrityGate.Decide(noManifestDev, devAllow: true) == GateDecision.Allow, "IntegrityGate allows a missing manifest only under the dev switch");

    // Unparseable manifest.
    var pkgBroken = FreshIntegrityPackage();
    File.WriteAllText(Path.Combine(pkgBroken, "approved_manifest.json"), "{ broken json");
    var brokenResult = IntegrityVerifier.VerifyPackage(pkgBroken, strict: true);
    context.AssertTrue(brokenResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier unparseable manifest fails closed");

    // Empty files list.
    var pkgEmpty = FreshIntegrityPackage();
    File.WriteAllText(Path.Combine(pkgEmpty, "approved_manifest.json"), "{\"version\":\"0.7.0\",\"files\":[]}");
    var emptyResult = IntegrityVerifier.VerifyPackage(pkgEmpty, strict: true);
    context.AssertTrue(emptyResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier empty manifest files list fails closed");

    // Corrupt-but-parseable manifest with a null entry must fail closed, not throw (PR #61 P2).
    var pkgNullEntry = FreshIntegrityPackage();
    File.WriteAllText(Path.Combine(pkgNullEntry, "approved_manifest.json"), "{\"version\":\"0.7.0\",\"files\":[null]}");
    var nullEntryResult = IntegrityVerifier.VerifyPackage(pkgNullEntry, strict: true);
    context.AssertTrue(nullEntryResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest with a null file entry fails closed without throwing");

    // Valid entries plus a trailing null entry must still fail closed (no early-return Ok).
    var pkgMixedNull = FreshIntegrityPackage();
    var mixedNullEntries = IntegrityEntries(pkgMixedNull);
    mixedNullEntries.Add(null!);
    WriteIntegrityManifest(pkgMixedNull, "0.7.0", mixedNullEntries);
    var mixedNullResult = IntegrityVerifier.VerifyPackage(pkgMixedNull, strict: true);
    context.AssertTrue(mixedNullResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest with a valid-plus-null entry list fails closed");

    // Mandatory file deleted while its manifest entry is tampered to required:false must still fail
    // closed (mandatory enforced by path, not by the manifest-controlled flag) (PR #61 P2).
    var pkgReqFalse = FreshIntegrityPackage();
    var reqFalseEntries = IntegrityEntries(pkgReqFalse);
    reqFalseEntries.First(x => (string)x["path"]! == "config/security_policy.json")["required"] = false;
    WriteIntegrityManifest(pkgReqFalse, "0.7.0", reqFalseEntries);
    File.Delete(Path.Combine(pkgReqFalse, "config", "security_policy.json"));
    var reqFalseResult = IntegrityVerifier.VerifyPackage(pkgReqFalse, strict: true);
    context.AssertTrue(reqFalseResult.Status == IntegrityStatus.FailClosed && reqFalseResult.BlockedClasses.Contains("Policy"), "IntegrityVerifier manifest mandatory file deleted with required:false still fails closed");

    // Windows-rooted path entry must be rejected on ANY host (Path.IsPathRooted misses C:/ on Linux) (PR #61 P2).
    var pkgWinRooted = FreshIntegrityPackage();
    var winRootedEntries = IntegrityEntries(pkgWinRooted);
    winRootedEntries.Add(new Dictionary<string, object?> { ["path"] = "C:/Windows/System32/evil.dll", ["size"] = 1L, ["sha256"] = "00", ["class"] = "App", ["required"] = false });
    WriteIntegrityManifest(pkgWinRooted, "0.7.0", winRootedEntries);
    var winRootedResult = IntegrityVerifier.VerifyPackage(pkgWinRooted, strict: true);
    context.AssertTrue(winRootedResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest Windows-rooted path entry fails closed on any host");

    // Malformed path (embedded NUL) makes Path.GetFullPath throw; must be caught and fail closed (PR #61 P2).
    var pkgMalformed = FreshIntegrityPackage();
    var malformedEntries = IntegrityEntries(pkgMalformed);
    malformedEntries.Add(new Dictionary<string, object?> { ["path"] = "bad\0name", ["size"] = 1L, ["sha256"] = "00", ["class"] = "App", ["required"] = false });
    WriteIntegrityManifest(pkgMalformed, "0.7.0", malformedEntries);
    var malformedResult = IntegrityVerifier.VerifyPackage(pkgMalformed, strict: true);
    context.AssertTrue(malformedResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest malformed path entry fails closed without throwing");

    // Version mismatch (manifest declares a different version than the Core constant).
    var pkgVersion = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgVersion, "9.9.9", IntegrityEntries(pkgVersion));
    var versionResult = IntegrityVerifier.VerifyPackage(pkgVersion, strict: true);
    context.AssertTrue(versionResult.Status == IntegrityStatus.FailClosed, "IntegrityVerifier manifest version mismatch fails closed");

    // Dev vs release: identical tamper allows under dev switch, blocks in release.
    var pkgDevTamper = FreshIntegrityPackage();
    WriteIntegrityManifest(pkgDevTamper, "0.7.0", IntegrityEntries(pkgDevTamper));
    File.WriteAllText(Path.Combine(pkgDevTamper, "config", "security_policy.json"), "tampered-under-dev");
    var devTamperResult = IntegrityVerifier.VerifyPackage(pkgDevTamper, strict: false);
    context.AssertTrue(devTamperResult.Status == IntegrityStatus.DevFallback && devTamperResult.UsedDevFallback, "IntegrityVerifier manifest tamper under dev switch downgrades to dev fallback");
    context.AssertTrue(IntegrityGate.Decide(devTamperResult, devAllow: true) == GateDecision.Allow, "IntegrityGate allows manifest tamper only under the dev switch");
    var devTamperStrict = IntegrityVerifier.VerifyPackage(pkgDevTamper, strict: true);
    context.AssertTrue(devTamperStrict.Status == IntegrityStatus.FailClosed, "IntegrityVerifier same manifest tamper fails closed in release mode");

    // Pure gate decisions.
    context.AssertTrue(IntegrityGate.Decide(IntegrityResult.NotVerified(), devAllow: false) == GateDecision.Block, "IntegrityGate blocks the NotVerified manifest state in release mode");
    context.AssertTrue(IntegrityGate.Decide(IntegrityResult.NotVerified(), devAllow: true) == GateDecision.Allow, "IntegrityGate allows the NotVerified manifest state only under the dev switch");
    var fabricatedOk = new IntegrityResult(IntegrityStatus.Ok, false, Array.Empty<string>(), Array.Empty<SafetyFinding>(), new HashSet<string>(StringComparer.Ordinal));
    context.AssertTrue(IntegrityGate.Decide(fabricatedOk, devAllow: false) == GateDecision.Allow, "IntegrityGate allows an Ok manifest result");

    // Manifest shrink of a NON-mandatory critical asset: drop the rules/* entry but leave the file on
    // disk (and tamper it). All six mandatory paths remain, so this must be caught by the critical-glob
    // scan, not the mandatory check (PR #61 P2).
    var pkgShrinkRules = FreshIntegrityPackage();
    var shrinkRulesEntries = IntegrityEntries(pkgShrinkRules).Where(x => (string)x["path"]! != "rules/sql_deny_patterns.txt").ToList();
    WriteIntegrityManifest(pkgShrinkRules, "0.7.0", shrinkRulesEntries);
    File.WriteAllText(Path.Combine(pkgShrinkRules, "rules", "sql_deny_patterns.txt"), "attacker-controlled-rule");
    var shrinkRulesResult = IntegrityVerifier.VerifyPackage(pkgShrinkRules, strict: true);
    context.AssertTrue(shrinkRulesResult.Status == IntegrityStatus.FailClosed && shrinkRulesResult.BlockedClasses.Contains("Rules"), "IntegrityVerifier manifest shrink of a non-mandatory critical rules asset fails closed (undeclared on-disk file)");

    // Dropping a NON-mandatory kb/*.csv entry while the file remains is likewise caught by the
    // critical-glob scan (isolates the scan from the mandatory check).
    var pkgShrinkKb = FreshIntegrityPackage();
    var shrinkKbEntries = IntegrityEntries(pkgShrinkKb).Where(x => (string)x["path"]! != "kb/public_regulation_catalog.csv").ToList();
    WriteIntegrityManifest(pkgShrinkKb, "0.7.0", shrinkKbEntries);
    var shrinkKbResult = IntegrityVerifier.VerifyPackage(pkgShrinkKb, strict: true);
    context.AssertTrue(shrinkKbResult.Status == IntegrityStatus.FailClosed && shrinkKbResult.BlockedClasses.Contains("Kb"), "IntegrityVerifier manifest shrink of a non-mandatory kb critical asset fails closed");

    // Critical-glob entry kept but required flipped to false AND the file deleted must still fail
    // closed: critical assets are required by path, not by the manifest-controlled flag (PR #61 P2).
    var pkgReqFalseGlob = FreshIntegrityPackage();
    var reqFalseGlobEntries = IntegrityEntries(pkgReqFalseGlob);
    reqFalseGlobEntries.First(x => (string)x["path"]! == "kb/public_regulation_catalog.csv")["required"] = false;
    WriteIntegrityManifest(pkgReqFalseGlob, "0.7.0", reqFalseGlobEntries);
    File.Delete(Path.Combine(pkgReqFalseGlob, "kb", "public_regulation_catalog.csv"));
    var reqFalseGlobResult = IntegrityVerifier.VerifyPackage(pkgReqFalseGlob, strict: true);
    context.AssertTrue(reqFalseGlobResult.Status == IntegrityStatus.FailClosed && reqFalseGlobResult.BlockedClasses.Contains("Kb"), "IntegrityVerifier manifest critical-glob asset deleted with required:false still fails closed (required by path)");

    // Mandatory asset removed from manifest AND deleted from disk still fails closed — the six
    // mandatory paths are anchored by the hard-coded declared-check even on co-deletion (PR #61 P2).
    var pkgMandatoryCoDel = FreshIntegrityPackage();
    var mandatoryCoDelEntries = IntegrityEntries(pkgMandatoryCoDel).Where(x => (string)x["path"]! != "config/security_policy.json").ToList();
    WriteIntegrityManifest(pkgMandatoryCoDel, "0.7.0", mandatoryCoDelEntries);
    File.Delete(Path.Combine(pkgMandatoryCoDel, "config", "security_policy.json"));
    var mandatoryCoDelResult = IntegrityVerifier.VerifyPackage(pkgMandatoryCoDel, strict: true);
    context.AssertTrue(mandatoryCoDelResult.Status == IntegrityStatus.FailClosed && mandatoryCoDelResult.BlockedClasses.Contains("Policy"), "IntegrityVerifier manifest mandatory asset co-deletion (entry removed + file deleted) fails closed");

    // NON-mandatory critical asset removed from BOTH manifest and disk (co-deletion) must fail closed
    // via RequiredCriticalEntries. This closes the manifest-shrink deletion gap before code signing.
    var pkgGlobCoDel = FreshIntegrityPackage();
    var globCoDelEntries = IntegrityEntries(pkgGlobCoDel).Where(x => (string)x["path"]! != "kb/public_regulation_catalog.csv").ToList();
    WriteIntegrityManifest(pkgGlobCoDel, "0.7.0", globCoDelEntries);
    File.Delete(Path.Combine(pkgGlobCoDel, "kb", "public_regulation_catalog.csv"));
    var globCoDelResult = IntegrityVerifier.VerifyPackage(pkgGlobCoDel, strict: true);
    context.AssertTrue(globCoDelResult.Status == IntegrityStatus.FailClosed && globCoDelResult.BlockedClasses.Contains("Kb"), "IntegrityVerifier manifest non-mandatory critical co-deletion (entry+file removed) fails closed via pinned critical entries");

    // Documented residual: an attacker who rewrites a file AND regenerates the folder manifest in
    // lock-step is NOT detected by the interim (no independent trust anchor). Deferred to code signing.
    var pkgCoTamper = FreshIntegrityPackage();
    File.WriteAllText(Path.Combine(pkgCoTamper, "config", "security_policy.json"), "attacker-controlled-policy");
    WriteIntegrityManifest(pkgCoTamper, "0.7.0", IntegrityEntries(pkgCoTamper));
    var coTamperResult = IntegrityVerifier.VerifyPackage(pkgCoTamper, strict: true);
    context.AssertTrue(coTamperResult.Status == IntegrityStatus.Ok, "IntegrityVerifier manifest co-tamper (file+manifest rewritten together) is NOT detected — documented residual deferred to code signing");

    // Lock-step with build/03 §4 and the VERSION single source of truth.
    var build03IntegrityText = File.ReadAllText(Path.Combine("build", "03_verify-package.ps1"));
    foreach (var mandatory in mandatoryEntries)
    {
        context.AssertTrue(build03IntegrityText.Contains(mandatory, StringComparison.Ordinal), $"build/03 manifest verification should enforce mandatory entry '{mandatory}' (lock-step with IntegrityVerifier)");
    }
    context.AssertTrue(build03IntegrityText.Contains("approved_manifest.json", StringComparison.Ordinal), "build/03 should verify the approved_manifest.json (lock-step with runtime IntegrityVerifier)");
    context.AssertTrue(build03IntegrityText.Contains("manifest path escapes package root", StringComparison.Ordinal), "build/03 manifest verification should guard path traversal (lock-step with IntegrityVerifier)");
    context.AssertTrue(IntegrityVerifier.MandatoryEntries.Count == 6, "IntegrityVerifier manifest mandatory entry set should contain the six core entries");
    context.AssertTrue(string.Equals(IntegrityVerifier.ExpectedVersion, File.ReadAllText("VERSION").Trim(), StringComparison.Ordinal), "IntegrityVerifier manifest ExpectedVersion should equal the VERSION file (single source of truth)");

    // The runtime critical-glob shrink guard must stay in lock-step with build/01 manifest generation.
    var build01IntegrityText = File.ReadAllText(Path.Combine("build", "01_publish-win-x64.ps1"));
    foreach (var globPattern in new[] { "rules", "templates", "config/ncr", "*.csv", "*.md", "*.json" })
    {
        context.AssertTrue(build01IntegrityText.Contains(globPattern, StringComparison.Ordinal), $"build/01 manifest generation should cover critical glob '{globPattern}' (lock-step with IntegrityVerifier critical-glob shrink guard)");
    }

    var repoRoot = Directory.GetCurrentDirectory();
    if (!repoRoot.EndsWith(Path.DirectorySeparatorChar))
    {
        repoRoot += Path.DirectorySeparatorChar;
    }

    var repoCriticalPaths = new List<string>();
    foreach (var (dir, pattern) in new[]
    {
        ("rules", "*"),
        ("templates", "*"),
        (Path.Combine("config", "ncr"), "*.json"),
        ("kb", "*.csv"),
        ("kb", "*.md")
    })
    {
        if (!Directory.Exists(dir))
        {
            continue;
        }

        repoCriticalPaths.AddRange(Directory.GetFiles(dir, pattern, SearchOption.AllDirectories)
            .Select(path => Path.GetFullPath(path).Substring(repoRoot.Length).Replace(Path.DirectorySeparatorChar, '/')));
    }

    var actualCriticalPaths = IntegrityVerifier.RequiredCriticalEntries.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    var expectedCriticalPaths = repoCriticalPaths.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    context.AssertTrue(actualCriticalPaths.SequenceEqual(expectedCriticalPaths), "IntegrityVerifier RequiredCriticalEntries should match the current build/01 critical asset inventory");

    foreach (var dir in integrityTempDirs)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception cleanupEx) when (cleanupEx is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup of temp packages.
        }
    }
    }
}
}
