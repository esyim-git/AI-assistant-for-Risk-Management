internal static class MappingTests
{
    internal static void Run(SmokeTestContext context)
    {
        var columnMappingLoadResult = ColumnMappingLoader.LoadDefault();
context.AssertTrue(!columnMappingLoadResult.UsedFallback, "ColumnMappingLoader should load repo default mapping");
context.AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMapping default should preserve BASE_DT");
context.AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORTFOLIO_ID", "ColumnMapping default should preserve PORTFOLIO_ID");

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