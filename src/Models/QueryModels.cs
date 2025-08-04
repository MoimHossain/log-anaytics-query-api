namespace LogAnalyticsQueryApi.Models;

public class QueryRequest
{
    public string TableName { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? AdditionalFilters { get; set; }
    public int? Top { get; set; } = 100;
}

public class QueryResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
    public int? RowCount { get; set; }
}

public class AzureAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}
