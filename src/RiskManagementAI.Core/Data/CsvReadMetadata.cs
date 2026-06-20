namespace RiskManagementAI.Core.Data;

public sealed record CsvReadMetadata(
    CsvEncoding RequestedEncoding,
    CsvEncoding DetectedEncoding,
    bool HadUtf8Bom,
    string? Cp949MappingSha256,
    int? Cp949MappingEntryCount);
