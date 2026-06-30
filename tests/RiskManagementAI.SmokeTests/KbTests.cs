internal static class KbTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
var kbSearchLogPath = Path.Combine("logs", "smoke_kb_search_log.jsonl");
if (File.Exists(kbSearchLogPath))
{
    File.Delete(kbSearchLogPath);
}

var regulationCatalog = RegulationCatalog.LoadDefault();
context.AssertTrue(regulationCatalog.Entries.Count >= 5, "RegulationCatalog should load public catalog entries");
var publicRegEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "FIA_REG");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.Source) && publicRegEntry.Source != publicRegEntry.SourceOrg, "RegulationCatalog metadata should include source locator distinct from source org");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.Version), "RegulationCatalog metadata should include version field");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.EffectiveDate), "RegulationCatalog metadata should include effective date field");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.LoadedDate), "RegulationCatalog metadata should include loaded date field");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.ApprovalStatus), "RegulationCatalog metadata should include approval status");
context.AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.LicenseStatus), "RegulationCatalog metadata should include license status");
context.AssertTrue(regulationCatalog.Warnings.Any(warning => warning.Contains("file_hash", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn when public source file hash is not loaded");
var internalCatalogEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "INTERNAL_RULES");
var ncrCatalogEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "NCR_GUIDE");
context.AssertTrue(internalCatalogEntry.Status == "PROD_ONLY" && internalCatalogEntry.Note.Contains("권한통제형", StringComparison.Ordinal), "RegulationCatalog should keep internal rules metadata-only and prod-only");
context.AssertTrue(ncrCatalogEntry.Status == "MANUAL_APPROVAL_REQUIRED" && ncrCatalogEntry.Note.Contains("원문 금지", StringComparison.Ordinal), "RegulationCatalog should keep NCR official text out of repo");

var legacyCatalogPath = Path.Combine(Path.GetTempPath(), $"legacy_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    legacyCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nLEGACY,PUBLIC_REG,Legacy Regulation,Public Org,Public regulation,CATALOG_ONLY,legacy only\n",
    Encoding.UTF8);
var legacyCatalog = RegulationCatalog.LoadFromFile(legacyCatalogPath);
context.AssertTrue(legacyCatalog.Entries.Single().Source == string.Empty, "RegulationCatalog should load legacy 7-column catalog with empty source metadata");
context.AssertTrue(legacyCatalog.Warnings.Any(warning => warning.Contains("missing optional column 'source'", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn on missing metadata columns without throwing");

var missingRequiredCatalogPath = Path.Combine(Path.GetTempPath(), $"missing_required_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    missingRequiredCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nSHORT,PUBLIC_REG,Short Regulation,Public Org,Public regulation,CATALOG_ONLY\n",
    Encoding.UTF8);
context.AssertTrue(context.Throws<InvalidDataException>(() => RegulationCatalog.LoadFromFile(missingRequiredCatalogPath)), "RegulationCatalog should reject rows that omit required catalog fields");

var emptyRequiredCatalogPath = Path.Combine(Path.GetTempPath(), $"empty_required_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    emptyRequiredCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nEMPTY_STATUS,PUBLIC_REG,Empty Status,Public Org,Public regulation,,required status is empty\n",
    Encoding.UTF8);
context.AssertTrue(context.Throws<InvalidDataException>(() => RegulationCatalog.LoadFromFile(emptyRequiredCatalogPath)), "RegulationCatalog should reject empty required catalog values");

var emptyMetadataCatalogPath = Path.Combine(Path.GetTempPath(), $"empty_metadata_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    emptyMetadataCatalogPath,
    "source_id,category,title,source_org,source_type,status,note,source,version,effective_date,repeal_date,file_hash,loaded_date,approval_status,superseded_by,license_status\nEMPTY,PUBLIC_REG,Empty Metadata,Public Org,Public regulation,CATALOG_ONLY,metadata empty,,,,,,,,,\n",
    Encoding.UTF8);
var emptyMetadataCatalog = RegulationCatalog.LoadFromFile(emptyMetadataCatalogPath);
context.AssertTrue(emptyMetadataCatalog.Entries.Single().LicenseStatus == string.Empty, "RegulationCatalog should keep empty metadata values as empty strings");
context.AssertTrue(emptyMetadataCatalog.Warnings.Any(warning => warning.Contains("metadata incomplete", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn on empty metadata values without throwing");
var kbSearch = new KbSearch(
    regulationCatalog,
    new TaskLogWriter("logs", "smoke_kb_search_log.jsonl"),
    loadedRuleSet.RuleVersion);
var ncrSearchResponse = kbSearch.Search("NCR", "user-smoke");
context.AssertTrue(ncrSearchResponse.Results.Any(result => result.SourceId == "NCR_GUIDE"), "KbSearch should find NCR catalog entry");
var ncrSearchResult = ncrSearchResponse.Results.Single(result => result.SourceId == "NCR_GUIDE");
context.AssertTrue(ncrSearchResult.Disclosure == KbDisclosure.ApprovalRequired, "KbSearch should mark NCR manual upload entries as approval required");
context.AssertTrue(ncrSearchResult.DisclosureReason.Contains("원문 미적재", StringComparison.Ordinal), "KbSearch should explain NCR source text is not loaded");
context.AssertTrue(ncrSearchResponse.Findings.Any(finding => finding.Code == "KB_APPROVAL_REQUIRED" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured approval-required finding for NCR entries");
context.AssertTrue(ncrSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal), "KbSearch answer should mark review draft");
context.AssertTrue(ncrSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch answer should always include sources");
context.AssertTrue(ncrSearchResponse.DraftAnswer.Contains("원문은 포함하지 않습니다", StringComparison.Ordinal), "KbSearch answer should state internal originals are excluded");
context.AssertTrue(!ncrSearchResponse.DraftAnswer.Contains("제1조", StringComparison.Ordinal), "KbSearch should not expose NCR clause source text");
context.AssertTrue(ncrSearchResponse.AuditLogWritten, "KbSearch should write audit log when configured");
context.AssertTrue(!ncrSearchResponse.DraftAnswer.Contains("NOT_LOADED", StringComparison.Ordinal), "KbSearch citation answer should not cite NOT_LOADED as real metadata");
context.AssertTrue(ncrSearchResponse.DraftAnswer.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render NOT_LOADED metadata as confirmation-needed");
context.AssertTrue(ncrSearchResponse.Warnings.Any(warning => warning.Contains("source_id=NCR_GUIDE", StringComparison.Ordinal) && warning.Contains("version", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for NOT_LOADED version metadata");

var publicRegSearchResponse = kbSearch.Search("금융투자업규정", "user-smoke");
context.AssertTrue(publicRegSearchResponse.Results.Any(result => result.SourceId == "FIA_REG"), "KbSearch should find public regulation catalog entry");
var publicRegSearchResult = publicRegSearchResponse.Results.First(result => result.SourceId == "FIA_REG");
context.AssertTrue(publicRegSearchResult.Disclosure == KbDisclosure.PublicCited, "KbSearch should mark public catalog entries as PublicCited");
context.AssertTrue(publicRegSearchResult.DisclosureReason.Contains("공개 catalog", StringComparison.Ordinal), "KbSearch should explain public catalog citation disclosure");
var internalRulesSearchResponse = kbSearch.Search("내부규정", "user-smoke");
var internalRulesSearchResult = internalRulesSearchResponse.Results.Single(result => result.SourceId == "INTERNAL_RULES");
context.AssertTrue(internalRulesSearchResult.Disclosure == KbDisclosure.MetadataOnly, "KbSearch should keep internal rules metadata-only");
context.AssertTrue(internalRulesSearchResult.DisclosureReason.Contains("Prod 권한통제", StringComparison.Ordinal), "KbSearch should route internal source text to Prod-controlled KB");
context.AssertTrue(internalRulesSearchResponse.Findings.Any(finding => finding.Code == "KB_PROD_ONLY_METADATA"), "KbSearch should emit structured metadata-only finding for internal rules");
var fixedKbSearch = new KbSearch(regulationCatalog, clock: new FixedClock(new DateOnly(2026, 6, 21)));
var citationSearchResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "2026-06-20");
var citationText = citationSearchResponse.DraftAnswer;
context.AssertTrue(citationText.Contains(publicRegEntry.Title, StringComparison.Ordinal), "KbSearch citation answer should include document name");
context.AssertTrue(citationText.Contains("버전", StringComparison.Ordinal) && citationText.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render placeholder version as confirmation-needed");
context.AssertTrue(citationText.Contains("시행일", StringComparison.Ordinal) && citationText.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render placeholder effective date as confirmation-needed");
context.AssertTrue(!citationText.Contains("CONFIRM_CURRENT_VERSION", StringComparison.Ordinal), "KbSearch citation answer should not cite placeholder version as real metadata");
context.AssertTrue(!citationText.Contains("CONFIRM_EFFECTIVE_DATE", StringComparison.Ordinal), "KbSearch citation answer should not cite placeholder effective date as real metadata");
context.AssertTrue(citationText.Contains("조항:", StringComparison.Ordinal), "KbSearch citation answer should include clause label");
context.AssertTrue(citationText.Contains(publicRegEntry.Source, StringComparison.Ordinal), "KbSearch citation answer should include source locator");
context.AssertTrue(citationText.Contains("검색 기준일: 2026-06-20", StringComparison.Ordinal), "KbSearch citation answer should include caller as-of date");
context.AssertTrue(citationText.Contains("검토 필요", StringComparison.Ordinal), "KbSearch citation answer should include review-needed wording");
context.AssertTrue(citationSearchResponse.Results.Any(result => result.SourceId == publicRegEntry.SourceId && result.Version == publicRegEntry.Version && result.Source == publicRegEntry.Source), "KbSearchResult should expose citation metadata");
context.AssertTrue(citationSearchResponse.Warnings.Any(warning => warning.Contains("source_id=FIA_REG", StringComparison.Ordinal) && warning.Contains("version", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for placeholder version metadata");
context.AssertTrue(citationSearchResponse.Warnings.Any(warning => warning.Contains("source_id=FIA_REG", StringComparison.Ordinal) && warning.Contains("effective_date", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for placeholder effective date metadata");
var repeatedCitationAnswer = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "2026-06-20").DraftAnswer;
context.AssertTrue(citationText == repeatedCitationAnswer, "KbSearch citation answer should be deterministic for the same as-of date");
var clockDateResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke");
context.AssertTrue(clockDateResponse.DraftAnswer.Contains("검색 기준일: 2026-06-21", StringComparison.Ordinal), "KbSearch citation answer should use injected clock date when asOfDate is omitted");
context.AssertTrue(!clockDateResponse.DraftAnswer.Contains("검색 기준일: (미기재)", StringComparison.Ordinal), "KbSearch citation answer should not use placeholder for search date");
var invalidAsOfDateResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "not-a-date");
context.AssertTrue(invalidAsOfDateResponse.DraftAnswer.Contains("검색 기준일: 2026-06-21", StringComparison.Ordinal), "KbSearch citation answer should fallback to injected clock date for invalid asOfDate");
context.AssertTrue(!invalidAsOfDateResponse.DraftAnswer.Contains("검색 기준일: not-a-date", StringComparison.Ordinal), "KbSearch citation answer should not cite invalid asOfDate text");
context.AssertTrue(invalidAsOfDateResponse.Warnings.Any(warning => warning.Contains("yyyy-MM-dd", StringComparison.Ordinal)), "KbSearch citation answer should warn on invalid asOfDate");
var emptyMetadataSearch = new KbSearch(emptyMetadataCatalog, clock: new FixedClock(new DateOnly(2026, 6, 21)));
var emptyMetadataSearchResponse = emptyMetadataSearch.Search("Empty Metadata", "user-smoke", asOfDate: "2026-06-20");
context.AssertTrue(emptyMetadataSearchResponse.DraftAnswer.Contains("(미기재)", StringComparison.Ordinal), "KbSearch citation answer should render empty metadata as missing gracefully");
context.AssertTrue(!emptyMetadataSearchResponse.DraftAnswer.Contains("검색 기준일: (미기재)", StringComparison.Ordinal), "KbSearch citation answer should never render search date as missing");
context.AssertTrue(emptyMetadataSearchResponse.Findings.Any(finding => finding.Code == "KB_LICENSE_MISSING" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured finding for missing license metadata");
context.AssertTrue(emptyMetadataSearchResponse.Findings.Any(finding => finding.Code == "KB_APPROVAL_MISSING" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured finding for missing approval metadata");
var unknownStatusCatalogPath = Path.Combine(Path.GetTempPath(), $"unknown_status_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    unknownStatusCatalogPath,
    "source_id,category,title,source_org,source_type,status,note,source,version,effective_date,repeal_date,file_hash,loaded_date,approval_status,superseded_by,license_status\nUNKNOWN_STATUS,PUBLIC_REG,Mystery Status,Public Org,Public regulation,MYSTERY_STATUS,unknown status metadata,https://example.invalid,v1,2026-01-01,,hash,2026-06-21,PUBLIC_CATALOG_METADATA,,PUBLIC_REFERENCE\n",
    Encoding.UTF8);
var unknownStatusSearchResponse = new KbSearch(RegulationCatalog.LoadFromFile(unknownStatusCatalogPath)).Search("Mystery Status", "user-smoke");
var unknownStatusResult = unknownStatusSearchResponse.Results.Single(result => result.SourceId == "UNKNOWN_STATUS");
context.AssertTrue(unknownStatusResult.Disclosure == KbDisclosure.MetadataOnly, "KbSearch should conservatively mark unknown status as metadata-only");
context.AssertTrue(unknownStatusSearchResponse.Findings.Any(finding => finding.Code == "KB_UNKNOWN_STATUS" && finding.Severity == SafetySeverity.High), "KbSearch should emit structured finding for unknown status");
var clauseSampleLoad = ClausePackLoader.LoadDefault();
context.AssertTrue(!clauseSampleLoad.UsedFallback, "KbSearch clause pack loader should load synthetic sample without fallback");
context.AssertTrue(clauseSampleLoad.Clauses.Count == 2, "KbSearch clause pack loader should load synthetic sample clauses");
var firstClause = clauseSampleLoad.Clauses.First();
context.AssertTrue(firstClause.ChunkId.StartsWith("clause-", StringComparison.Ordinal) && firstClause.ChunkId.Length == 19, "KbSearch clause pack ChunkId should use deterministic clause prefix");
context.AssertTrue(LogHash.IsSha256Hex(firstClause.SourceTextHash), "KbSearch clause pack SourceTextHash should be SHA256 hex");
context.AssertTrue(
    clauseSampleLoad.Clauses.Select(clause => clause.ChunkId).SequenceEqual(ClausePackLoader.LoadDefault().Clauses.Select(clause => clause.ChunkId)),
    "KbSearch clause pack loader should produce deterministic ChunkIds for the same sample");
var missingClausePack = ClausePackLoader.Load("kb/clause_pack_sample/missing_clause_pack.csv");
context.AssertTrue(missingClausePack.UsedFallback && missingClausePack.Clauses.Count == 0 && missingClausePack.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_MISSING"), "KbSearch clause pack loader should safe-fallback with diagnostics when pack is missing");
var rejectedClausePackPath = ClausePackLoader.Load("kb/../clause_pack.csv");
context.AssertTrue(rejectedClausePackPath.UsedFallback && rejectedClausePackPath.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_PATH_REJECTED"), "KbSearch clause pack loader should reject traversal paths with diagnostics");
var rootedClausePackPath = ClausePackLoader.Load(Path.GetFullPath(ClausePackLoader.DefaultSamplePath));
context.AssertTrue(rootedClausePackPath.UsedFallback && rootedClausePackPath.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_PATH_REJECTED"), "KbSearch clause pack loader should reject rooted paths with diagnostics");
var nonCsvClausePackPath = ClausePackLoader.Load("kb/clause_pack_sample/not_csv.txt");
context.AssertTrue(nonCsvClausePackPath.UsedFallback && nonCsvClausePackPath.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_PATH_REJECTED"), "KbSearch clause pack loader should reject non-CSV packs with diagnostics");
var clausePackSampleText = File.ReadAllText(ClausePackLoader.DefaultSamplePath);
foreach (var token in PrivateGuardStrings("SuspiciousNameTokens").Concat(PrivateGuardStrings("SuspiciousContentTokens")))
{
    context.AssertTrue(!clausePackSampleText.Contains(token, StringComparison.OrdinalIgnoreCase), $"KbSearch clause pack synthetic sample should avoid suspicious token '{token}'");
}
var tempClausePackPaths = new List<string>();
try
{
    var duplicateClausePackPath = Path.Combine("kb", "clause_pack_sample", $"smoke_duplicate_{Guid.NewGuid():N}.csv");
    tempClausePackPaths.Add(duplicateClausePackPath);
    File.WriteAllText(
        duplicateClausePackPath,
        "clause_ref,clause_body,source_id,effective_date,repeal_date,pack_version\nArticle-X,alpha synthetic body,FIA_REG,2026-01-01,,pack-smoke\nArticle-X,beta synthetic body,FIA_REG,2026-01-01,,pack-smoke\n",
        Encoding.UTF8);
    var duplicateClauseLoad = ClausePackLoader.Load(duplicateClausePackPath);
    context.AssertTrue(duplicateClauseLoad.Clauses.Count == 1 && duplicateClauseLoad.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_DUPLICATE_NATURAL_KEY"), "KbSearch clause pack loader should reject conflicting duplicate natural keys with diagnostics");

    var missingHeaderClausePackPath = Path.Combine("kb", "clause_pack_sample", $"smoke_missing_header_{Guid.NewGuid():N}.csv");
    tempClausePackPaths.Add(missingHeaderClausePackPath);
    File.WriteAllText(
        missingHeaderClausePackPath,
        "clause_ref,clause_body,source_id,effective_date,repeal_date\nArticle-Y,alpha synthetic body,FIA_REG,2026-01-01,\n",
        Encoding.UTF8);
    var missingHeaderLoad = ClausePackLoader.Load(missingHeaderClausePackPath);
    context.AssertTrue(missingHeaderLoad.UsedFallback && missingHeaderLoad.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_HEADER_MISSING"), "KbSearch clause pack loader should safe-fallback when required headers are missing");

    var skippedRowClausePackPath = Path.Combine("kb", "clause_pack_sample", $"smoke_skipped_row_{Guid.NewGuid():N}.csv");
    tempClausePackPaths.Add(skippedRowClausePackPath);
    File.WriteAllText(
        skippedRowClausePackPath,
        "clause_ref,clause_body,source_id,effective_date,repeal_date,pack_version\n,alpha synthetic body,FIA_REG,2026-01-01,,pack-smoke\nArticle-Z,beta synthetic body,FIA_REG,2026-01-01,,pack-smoke\n",
        Encoding.UTF8);
    var skippedRowLoad = ClausePackLoader.Load(skippedRowClausePackPath);
    context.AssertTrue(skippedRowLoad.Clauses.Count == 1 && !skippedRowLoad.UsedFallback && skippedRowLoad.Findings.Any(finding => finding.Code == "KB_CLAUSE_PACK_ROW_SKIPPED"), "KbSearch clause pack loader should skip invalid rows with diagnostics while loading valid rows");
}
finally
{
    foreach (var path in tempClausePackPaths)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
var repoGuardFindings = KbRepositoryGuard.Scan(Directory.GetCurrentDirectory());
context.AssertTrue(!repoGuardFindings.Any(finding => finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should not find internal/NCR original text in repo assets");
var suspiciousKbRoot = Path.Combine(Path.GetTempPath(), $"kb_guard_{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(suspiciousKbRoot, "kb"));
File.WriteAllText(Path.Combine(suspiciousKbRoot, "kb", "internal_rule_original.txt"), "내부규정 원문", Encoding.UTF8);
var suspiciousFindings = KbRepositoryGuard.Scan(suspiciousKbRoot);
context.AssertTrue(suspiciousFindings.Any(finding => finding.Code == "KB_FORBIDDEN_SOURCE_TEXT" && finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should block suspicious internal/NCR original files");
var suspiciousNcrRoot = Path.Combine(Path.GetTempPath(), $"ncr_guard_{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(suspiciousNcrRoot, "config", "ncr"));
File.WriteAllText(Path.Combine(suspiciousNcrRoot, "config", "ncr", "ncr_official_original.json"), "official text", Encoding.UTF8);
var suspiciousNcrFindings = KbRepositoryGuard.Scan(suspiciousNcrRoot);
context.AssertTrue(suspiciousNcrFindings.Any(finding => finding.Code == "KB_FORBIDDEN_SOURCE_TEXT" && finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should scan config/ncr and block suspicious NCR originals");
var build03ScriptText = File.ReadAllText(Path.Combine("build", "03_verify-package.ps1"));
foreach (var token in PrivateGuardStrings("SuspiciousContentTokens"))
{
    if (token.All(ch => ch <= 0x7F))
    {
        context.AssertTrue(build03ScriptText.Contains(token, StringComparison.Ordinal), $"build/03 source-text scan should mirror ASCII content token '{token}'");
    }
    else
    {
        foreach (var codeUnit in token.Select(ch => $"0x{(int)ch:X4}").Distinct())
        {
            context.AssertTrue(build03ScriptText.Contains(codeUnit, StringComparison.OrdinalIgnoreCase), $"build/03 source-text scan should mirror non-ASCII content token '{token}' via code unit {codeUnit}");
        }
    }
}

foreach (var token in PrivateGuardStrings("SuspiciousNameTokens"))
{
    context.AssertTrue(build03ScriptText.Contains(token, StringComparison.Ordinal), $"build/03 source-text scan should mirror filename token '{token}'");
}

foreach (var allowlistPath in PrivateGuardStrings("MetadataAllowlist"))
{
    context.AssertTrue(build03ScriptText.Contains(allowlistPath, StringComparison.Ordinal), $"build/03 source-text scan should mirror allowlist path '{allowlistPath}'");
}

foreach (var extension in new[] { ".csv", ".json", ".jsonl", ".md", ".txt", ".sql" })
{
    context.AssertTrue(build03ScriptText.Contains(extension, StringComparison.Ordinal), $"build/03 source-text scan should include text extension '{extension}'");
}

foreach (var scanDirectory in new[] { "kb", "config", "samples", "data_sources" })
{
    context.AssertTrue(build03ScriptText.Contains(scanDirectory, StringComparison.Ordinal), $"build/03 source-text scan should include scan directory '{scanDirectory}'");
}

context.AssertTrue(build03ScriptText.Contains("Expand-Archive", StringComparison.Ordinal), "build/03 source-text scan should inspect extracted ZIP contents");
context.AssertTrue(build03ScriptText.Contains("New-StringFromCodeUnits", StringComparison.Ordinal), "build/03 source-text scan should avoid BOM-dependent non-ASCII PowerShell literals");
context.AssertTrue(build03ScriptText.Contains("Get-ZipRelativePath", StringComparison.Ordinal), "build/03 source-text scan should use a Windows PowerShell compatible relative path helper");
context.AssertTrue(!build03ScriptText.Contains("GetRelativePath", StringComparison.Ordinal), "build/03 source-text scan should not use .NET Core-only Path.GetRelativePath");
context.AssertTrue(build03ScriptText.Contains("GetEncoding(949)", StringComparison.Ordinal), "build/03 source-text scan should attempt CP949 decoding independently");
context.AssertTrue(build03ScriptText.Contains("PACKAGE SOURCE-TEXT VERIFICATION FAILED", StringComparison.Ordinal), "build/03 source-text scan should fail packaging on suspicious source text");

var kbIndexA = KbIndex.Build(regulationCatalog.Entries);
var kbIndexB = KbIndex.Build(regulationCatalog.Entries);
context.AssertTrue(kbIndexA.IndexedTermCount > regulationCatalog.Entries.Count, "KbIndex should build searchable inverted terms");
context.AssertTrue(kbIndexA.DeterministicSignature() == kbIndexB.DeterministicSignature(), "KbIndex build should be deterministic for the same catalog");
context.AssertTrue(kbIndexA.FindCandidates("투자업").Any(entry => entry.SourceId == "FIA_REG"), "KbIndex should preserve Korean substring candidates");
var longKbText = string.Concat(Enumerable.Range(0, 5000).Select(index => (char)('\uAC00' + index)));
var longKbEntry = publicRegEntry with
{
    SourceId = "LONG_NOTE",
    Note = longKbText
};
var longKbIndex = KbIndex.Build([longKbEntry]);
context.AssertTrue(longKbIndex.PostingCount < longKbText.Length * 40, "KbIndex should cap substring key generation for long catalog fields");
context.AssertTrue(longKbIndex.FindCandidates(longKbText.Substring(200, 12)).Any(entry => entry.SourceId == "LONG_NOTE"), "KbIndex bounded substrings should preserve long substring candidates");
var longQuery = new string('가', 5000);
context.AssertTrue(kbIndexA.FindCandidates(longQuery).Count == regulationCatalog.Entries.Count, "KbIndex should use full-catalog fallback for queries longer than the substring cap");
context.AssertTrue(
    ExpectedKbLinearResults(regulationCatalog, longQuery).SequenceEqual(KbSearchSignature(kbSearch.Search(longQuery, "user-smoke"))),
    "KbSearch long-query fallback should preserve linear scoring without unbounded substring expansion");
foreach (var query in new[]
         {
             "NCR",
             "금융투자업규정",
             "투자업",
             "Public regulation",
             "CATALOG_ONLY",
             "원문 금지",
             "법률 국가",
             "없는검색어",
             string.Empty
         })
{
    var expected = ExpectedKbLinearResults(regulationCatalog, query);
    var actual = KbSearchSignature(kbSearch.Search(query, "user-smoke"));
    context.AssertTrue(expected.SequenceEqual(actual), $"KbSearch indexed results should match linear scoring for query '{query}'");
}

var emptyKbSearchResponse = kbSearch.Search("없는검색어", "user-smoke");
context.AssertTrue(emptyKbSearchResponse.Results.Count == 0, "KbSearch should return zero results for unmatched query");
context.AssertTrue(emptyKbSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal) && emptyKbSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch no-result answer should still include review draft and source");
var kbSearchLogText = File.ReadAllText(kbSearchLogPath);
context.AssertTrue(!kbSearchLogText.Contains("NCR", StringComparison.Ordinal), "KbSearch audit should not store raw query text");
context.AssertTrue(!kbSearchLogText.Contains("user-smoke", StringComparison.Ordinal), "KbSearch audit should not store raw user id");
    }
}
