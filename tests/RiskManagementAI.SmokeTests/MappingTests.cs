internal static class MappingTests
{
    internal static void Run(SmokeTestContext context)
    {
        var columnMappingLoadResult = ColumnMappingLoader.LoadDefault();
context.AssertTrue(!columnMappingLoadResult.UsedFallback, "ColumnMappingLoader should load repo default mapping");
context.AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMapping default should preserve BASE_DT");
context.AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORTFOLIO_ID", "ColumnMapping default should preserve PORTFOLIO_ID");
context.AssertTrue(
    columnMappingLoadResult.Mapping.TryPhysical(LogicalColumn.CurrencyCode, out var defaultCurrencyColumn)
        && defaultCurrencyColumn == "CCY_CD"
        && columnMappingLoadResult.Mapping.TryPhysical(LogicalColumn.UnitCode, out var defaultUnitColumn)
        && defaultUnitColumn == "UNIT_CD",
    "ColumnMapping default should expose optional currency and unit physical columns");

var customColumnMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_custom.json");
File.WriteAllText(customColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "EXPOSURE",
    "LimitAmount": "LIMIT",
    "UseYn": "ACTIVE_YN"
  }
}
""");
var customColumnMappingResult = ColumnMappingLoader.LoadFromFile(customColumnMappingPath);
File.Delete(customColumnMappingPath);
context.AssertTrue(!customColumnMappingResult.UsedFallback, "ColumnMappingLoader should load complete custom config mapping");
context.AssertTrue(customColumnMappingResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORT_ID", "ColumnMapping custom config should apply physical column names");
context.AssertTrue(
    !customColumnMappingResult.Mapping.TryPhysical(LogicalColumn.CurrencyCode, out _)
        && !customColumnMappingResult.Mapping.TryPhysical(LogicalColumn.UnitCode, out _),
    "ColumnMappingLoader should treat optional currency and unit mappings as absent without fallback for six-column configs");

var blankOptionalColumnMappingPath = Path.Combine("config", "smoke_column_mapping_r2_blank_optional.json");
File.WriteAllText(blankOptionalColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "EXPOSURE",
    "LimitAmount": "LIMIT",
    "UseYn": "ACTIVE_YN",
    "CurrencyCode": "",
    "UnitCode": ""
  }
}
""");
var blankOptionalColumnMappingResult = ColumnMappingLoader.LoadFromFile(blankOptionalColumnMappingPath);
File.Delete(blankOptionalColumnMappingPath);
context.AssertTrue(!blankOptionalColumnMappingResult.UsedFallback, "ColumnMappingLoader should treat blank optional currency and unit mappings as absent without fallback");
context.AssertTrue(
    blankOptionalColumnMappingResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORT_ID"
        && !blankOptionalColumnMappingResult.Mapping.TryPhysical(LogicalColumn.CurrencyCode, out _)
        && !blankOptionalColumnMappingResult.Mapping.TryPhysical(LogicalColumn.UnitCode, out _),
    "ColumnMapping blank optional values should preserve required custom mappings and disable optional comparisons");

var partialColumnMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_partial.json");
File.WriteAllText(partialColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE"
  }
}
""");
var partialColumnMappingResult = ColumnMappingLoader.LoadFromFile(partialColumnMappingPath);
File.Delete(partialColumnMappingPath);
context.AssertTrue(partialColumnMappingResult.UsedFallback && partialColumnMappingResult.Warnings.Count > 0, "ColumnMappingLoader should fallback for partial custom mappings");
context.AssertTrue(partialColumnMappingResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMapping partial fallback should discard partial overrides");

var missingColumnMappingResult = ColumnMappingLoader.LoadFromFile($"config/smoke_column_mapping_missing_{Guid.NewGuid():N}.json");
context.AssertTrue(missingColumnMappingResult.UsedFallback && missingColumnMappingResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMappingLoader should safe fallback for missing mapping config");

var unknownLogicalColumnMappingPath = Path.Combine("config", "smoke_column_mapping_unknown_logical.json");
File.WriteAllText(unknownLogicalColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "EXPOSURE",
    "LimitAmount": "LIMIT",
    "UseYn": "ACTIVE_YN",
    "UnexpectedColumn": "SHOULD_NOT_LOAD"
  }
}
""");
var unknownLogicalColumnMappingResult = ColumnMappingLoader.LoadFromFile(unknownLogicalColumnMappingPath);
File.Delete(unknownLogicalColumnMappingPath);
context.AssertTrue(unknownLogicalColumnMappingResult.UsedFallback && unknownLogicalColumnMappingResult.Warnings.Any(warning => warning.Contains("unknown logical column", StringComparison.OrdinalIgnoreCase)), "ColumnMappingLoader should fallback on unknown logical mapping columns");

var nonStringPhysicalColumnMappingPath = Path.Combine("config", "smoke_column_mapping_non_string_physical.json");
File.WriteAllText(nonStringPhysicalColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": 20260617,
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "EXPOSURE",
    "LimitAmount": "LIMIT",
    "UseYn": "ACTIVE_YN"
  }
}
""");
var nonStringPhysicalColumnMappingResult = ColumnMappingLoader.LoadFromFile(nonStringPhysicalColumnMappingPath);
File.Delete(nonStringPhysicalColumnMappingPath);
context.AssertTrue(nonStringPhysicalColumnMappingResult.UsedFallback && nonStringPhysicalColumnMappingResult.Warnings.Any(warning => warning.Contains("must be a string physical column name", StringComparison.OrdinalIgnoreCase)), "ColumnMappingLoader should fallback on non-string physical column values");

var duplicatePhysicalMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_duplicate.json");
File.WriteAllText(duplicatePhysicalMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "DUPLICATE_AMOUNT",
    "LimitAmount": "DUPLICATE_AMOUNT",
    "UseYn": "ACTIVE_YN"
  }
}
""");
var duplicatePhysicalMappingResult = ColumnMappingLoader.LoadFromFile(duplicatePhysicalMappingPath);
File.Delete(duplicatePhysicalMappingPath);
context.AssertTrue(duplicatePhysicalMappingResult.UsedFallback && duplicatePhysicalMappingResult.Warnings.Count > 0, "ColumnMappingLoader should fallback on physical column collisions");
context.AssertTrue(context.Throws<ArgumentException>(() => ColumnMappingLoader.LoadFromFile("../x.json")), "ColumnMappingLoader should reject parent traversal paths");
context.AssertTrue(context.Throws<ArgumentException>(() => ColumnMappingLoader.LoadFromFile("artifacts/x.json")), "ColumnMappingLoader should reject paths outside config");
context.AssertTrue(context.Throws<InvalidDataException>(() => new ColumnMapping(new Dictionary<LogicalColumn, string>()).Physical(LogicalColumn.BaseDate)), "ColumnMapping should throw when a logical column is unmapped");
    }
}
