internal static class NcrTests
{
    internal static void Run(SmokeTestContext context)
    {
        var ncrRuleSetLoadResult = NcrRuleSetLoader.LoadDefault();
context.AssertTrue(!ncrRuleSetLoadResult.UsedFallback, "NcrRuleSetLoader should load repo sample structure");
context.AssertTrue(ncrRuleSetLoadResult.RuleSet.Components.Count > 0, "NcrRuleSet should include Components");
context.AssertTrue(ncrRuleSetLoadResult.RuleSet.ComponentMap.Count > 0, "NcrRuleSet should include ComponentMap");
context.AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RuleSetId), "NcrRuleSet should include RuleSetId");
context.AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RuleSetVersion), "NcrRuleSet should include RuleSetVersion");
context.AssertTrue(DateOnly.TryParseExact(ncrRuleSetLoadResult.RuleSet.EffectiveDate, "yyyy-MM-dd", out _), "NcrRuleSet should include YYYY-MM-DD EffectiveDate");
context.AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.FormulaDescription), "NcrRuleSet should include FormulaDescription");
context.AssertTrue(ncrRuleSetLoadResult.RuleSet.ValidationSqlTemplates.Count > 0, "NcrRuleSet should include Validation SQL templates");
context.AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RegulationBasis), "NcrRuleSet should include RegulationBasis");
context.AssertTrue(ncrRuleSetLoadResult.RuleSet.ApprovalHistory.Count > 0, "NcrRuleSet should include ApprovalHistory");
context.AssertTrue(!ncrRuleSetLoadResult.Findings.Any(finding => finding.Code.StartsWith("SQL_", StringComparison.Ordinal)), "NcrRuleSet sample validation SQL should be read-only");
context.AssertTrue(ncrRuleSetLoadResult.RuleSet.Components.All(component => component.ValuePolicy.Contains("APPROVAL_REQUIRED", StringComparison.Ordinal)), "NcrRuleSet sample should not contain real NCR coefficients");

var ncrExplanation = NcrExplain.Build(ncrRuleSetLoadResult.RuleSet);
context.AssertTrue(ncrExplanation.Contains("검토용 초안", StringComparison.Ordinal), "NcrExplain should always mark answers as review drafts");
context.AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.RuleSetVersion, StringComparison.Ordinal), "NcrExplain should include RuleSetVersion");
context.AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.EffectiveDate, StringComparison.Ordinal), "NcrExplain should include EffectiveDate");
context.AssertTrue(ncrExplanation.Contains("Component Map", StringComparison.Ordinal), "NcrExplain should include ComponentMap");
context.AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.FormulaDescription, StringComparison.Ordinal), "NcrExplain should include FormulaDescription");
context.AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.RegulationBasis, StringComparison.Ordinal), "NcrExplain should include RegulationBasis");

var missingNcrRuleSetResult = NcrRuleSetLoader.LoadFromFile("config/ncr/missing_smoke_ruleset.json");
context.AssertTrue(missingNcrRuleSetResult.UsedFallback && missingNcrRuleSetResult.Findings.Any(finding => finding.Code == "NCR_RULESET_MISSING"), "NcrRuleSetLoader should safe-fallback on missing files");
var rejectedNcrPathResult = NcrRuleSetLoader.LoadFromFile("../ncr_ruleset.json");
context.AssertTrue(rejectedNcrPathResult.UsedFallback && rejectedNcrPathResult.Findings.Any(finding => finding.Code == "NCR_RULESET_PATH_REJECTED"), "NcrRuleSetLoader should reject paths outside config/ncr");

var invalidNcrRelativePath = $"config/ncr/invalid_{Guid.NewGuid():N}.json";
Directory.CreateDirectory(Path.Combine("config", "ncr"));
File.WriteAllText(invalidNcrRelativePath, "{ broken json", Encoding.UTF8);
var invalidNcrResult = NcrRuleSetLoader.LoadFromFile(invalidNcrRelativePath);
context.AssertTrue(invalidNcrResult.UsedFallback && invalidNcrResult.Findings.Any(finding => finding.Code == "NCR_RULESET_LOAD_FAILED"), "NcrRuleSetLoader should safe-fallback on invalid JSON");
File.Delete(invalidNcrRelativePath);

var blockedSqlNcrRelativePath = $"config/ncr/blocked_sql_{Guid.NewGuid():N}.json";
File.WriteAllText(
    blockedSqlNcrRelativePath,
    """
{
  "RuleSetId": "BAD_NCR_RULESET",
  "RuleSetVersion": "bad-001",
  "EffectiveDate": "2026-06-21",
  "Components": [
    {
      "ComponentId": "BAD_COMPONENT",
      "Name": "Blocked component",
      "Category": "PLACEHOLDER",
      "ValuePolicy": "APPROVAL_REQUIRED_NO_REAL_COEFFICIENT"
    }
  ],
  "ComponentMap": [
    {
      "ComponentId": "BAD_COMPONENT",
      "SourceName": "APPROVED_SOURCE",
      "ColumnName": "APPROVED_COLUMN",
      "DataType": "DECIMAL",
      "Required": true
    }
  ],
  "FormulaDescription": "Structure-only description.",
  "ValidationSqlTemplates": [
    "DELETE FROM ncr_result WHERE base_dt = @BASE_DT"
  ],
  "RegulationBasis": "APPROVED_BASIS_REQUIRED",
  "ApprovalHistory": [
    {
      "Status": "TEST_ONLY",
      "ReviewerRole": "TEST",
      "ApprovedAt": "2026-06-21T00:00:00Z",
      "Note": "Test only."
    }
  ]
}
""",
    Encoding.UTF8);
var blockedSqlNcrResult = NcrRuleSetLoader.LoadFromFile(blockedSqlNcrRelativePath);
context.AssertTrue(blockedSqlNcrResult.UsedFallback && blockedSqlNcrResult.Findings.Any(finding => finding.Code == "SQL_DML_DELETE"), "NcrRuleSetLoader should flag blocked DML in validation SQL templates");
File.Delete(blockedSqlNcrRelativePath);
    }
}
