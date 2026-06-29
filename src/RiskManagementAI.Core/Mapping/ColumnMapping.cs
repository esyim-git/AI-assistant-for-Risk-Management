namespace RiskManagementAI.Core.Mapping;

public enum LogicalColumn
{
    BaseDate,
    PortfolioId,
    RiskFactor,
    ExposureAmount,
    LimitAmount,
    UseYn,
    CurrencyCode,
    UnitCode
}

public sealed class ColumnMapping
{
    private readonly IReadOnlyDictionary<LogicalColumn, string> physicalColumns;

    public ColumnMapping(IReadOnlyDictionary<LogicalColumn, string> physicalColumns)
    {
        ArgumentNullException.ThrowIfNull(physicalColumns);
        this.physicalColumns = new Dictionary<LogicalColumn, string>(physicalColumns);
    }

    public IReadOnlyDictionary<LogicalColumn, string> PhysicalColumns => physicalColumns;

    public static ColumnMapping SafeDefaults()
        => new(new Dictionary<LogicalColumn, string>
        {
            [LogicalColumn.BaseDate] = "BASE_DT",
            [LogicalColumn.PortfolioId] = "PORTFOLIO_ID",
            [LogicalColumn.RiskFactor] = "RISK_FACTOR",
            [LogicalColumn.ExposureAmount] = "EXPOSURE_AMT",
            [LogicalColumn.LimitAmount] = "LIMIT_AMT",
            [LogicalColumn.UseYn] = "USE_YN",
            [LogicalColumn.CurrencyCode] = "CCY_CD",
            [LogicalColumn.UnitCode] = "UNIT_CD"
        });

    public string Physical(LogicalColumn col)
    {
        if (physicalColumns.TryGetValue(col, out var physicalColumn)
            && !string.IsNullOrWhiteSpace(physicalColumn))
        {
            return physicalColumn;
        }

        throw new InvalidDataException($"Logical column '{col}' is not mapped to a physical column.");
    }

    public bool TryPhysical(LogicalColumn col, out string? physicalColumn)
    {
        if (physicalColumns.TryGetValue(col, out var mappedColumn)
            && !string.IsNullOrWhiteSpace(mappedColumn))
        {
            physicalColumn = mappedColumn;
            return true;
        }

        physicalColumn = null;
        return false;
    }
}

public sealed record ColumnMappingLoadResult(
    ColumnMapping Mapping,
    bool UsedFallback,
    IReadOnlyList<string> Warnings
);
