namespace RiskManagementAI.Core.Config;

public sealed record SecurityPolicy
{
    public NetworkPolicy Network { get; init; } = new();

    public SqlPolicy Sql { get; init; } = new();

    public VbaPolicy Vba { get; init; } = new();

    public DataPolicy Data { get; init; } = new();

    public static SecurityPolicy SafeDefaults()
    {
        return new SecurityPolicy();
    }

    public void EnsureExternalApiAllowed()
    {
        if (!Network.AllowExternalApi)
        {
            throw new InvalidOperationException("보안 정책에 따라 외부 API 호출은 차단되어 있습니다.");
        }
    }

    public void EnsureSqlAutoExecuteAllowed()
    {
        if (!Sql.AllowAutoExecute)
        {
            throw new InvalidOperationException("보안 정책에 따라 SQL 자동 실행은 차단되어 있습니다.");
        }
    }

    public void EnsureVbaAutoExecuteAllowed()
    {
        if (!Vba.AllowAutoExecute)
        {
            throw new InvalidOperationException("보안 정책에 따라 VBA 자동 실행은 차단되어 있습니다.");
        }
    }
}

public sealed record NetworkPolicy
{
    public bool AllowExternalApi { get; init; }

    public bool AllowAutoUpdate { get; init; }

    public bool AllowTelemetry { get; init; }
}

public sealed record SqlPolicy
{
    public bool AllowAutoExecute { get; init; }

    public string DefaultMode { get; init; } = "ReadOnlyDraft";
}

public sealed record VbaPolicy
{
    public bool AllowAutoExecute { get; init; }

    public bool BlockDangerousApi { get; init; } = true;
}

public sealed record DataPolicy
{
    public bool AllowRealDataInRepo { get; init; }

    public bool AllowInternalRegulationOriginalInRepo { get; init; }

    public bool AllowModelFileInRepo { get; init; }
}
