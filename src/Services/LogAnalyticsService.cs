using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using LogAnalyticsQueryApi.Models;

namespace LogAnalyticsQueryApi.Services;

public interface ILogAnalyticsService
{
    Task<QueryResponse> QueryLogAnalyticsAsync(QueryRequest request);
}

public class LogAnalyticsService : ILogAnalyticsService
{
    private readonly LogsQueryClient _logsQueryClient;
    private readonly ILogger<LogAnalyticsService> _logger;

    public LogAnalyticsService(LogsQueryClient logsQueryClient, ILogger<LogAnalyticsService> logger)
    {
        _logsQueryClient = logsQueryClient;
        _logger = logger;
    }

    public async Task<QueryResponse> QueryLogAnalyticsAsync(QueryRequest request)
    {
        try
        {
            // Build the KQL query
            var query = BuildKqlQuery(request);
            
            _logger.LogInformation("Executing query: {Query}", query);

            // Execute the query
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                request.WorkspaceId,
                query,
                new QueryTimeRange(request.StartTime, request.EndTime));

            var table = response.Value.Table;
            var results = ConvertTableToObjects(table);

            return new QueryResponse
            {
                Success = true,
                Data = results,
                RowCount = table.Rows.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Log Analytics query");
            return new QueryResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string BuildKqlQuery(QueryRequest request)
    {
        var query = request.TableName;
        
        // Add time range filter
        query += $" | where TimeGenerated >= datetime({request.StartTime:yyyy-MM-ddTHH:mm:ssZ})";
        query += $" | where TimeGenerated <= datetime({request.EndTime:yyyy-MM-ddTHH:mm:ssZ})";
        
        // Add additional filters if provided
        if (!string.IsNullOrWhiteSpace(request.AdditionalFilters))
        {
            query += $" | {request.AdditionalFilters}";
        }
        
        // Add top limit
        if (request.Top.HasValue && request.Top.Value > 0)
        {
            query += $" | take {request.Top.Value}";
        }

        return query;
    }

    private List<Dictionary<string, object?>> ConvertTableToObjects(LogsTable table)
    {
        var results = new List<Dictionary<string, object?>>();

        foreach (var row in table.Rows)
        {
            var obj = new Dictionary<string, object?>();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                var value = row[i];
                obj[column.Name] = value;
            }
            results.Add(obj);
        }

        return results;
    }
}
