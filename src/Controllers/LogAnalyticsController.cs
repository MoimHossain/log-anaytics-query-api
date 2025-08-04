using Microsoft.AspNetCore.Mvc;
using LogAnalyticsQueryApi.Models;
using LogAnalyticsQueryApi.Services;

namespace LogAnalyticsQueryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogAnalyticsController : ControllerBase
{
    private readonly ILogAnalyticsService _logAnalyticsService;
    private readonly ILogger<LogAnalyticsController> _logger;

    public LogAnalyticsController(ILogAnalyticsService logAnalyticsService, ILogger<LogAnalyticsController> logger)
    {
        _logAnalyticsService = logAnalyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Query Log Analytics workspace
    /// </summary>
    /// <param name="request">Query parameters including table name, workspace ID, and time range</param>
    /// <returns>Query results</returns>
    [HttpPost("query")]
    public async Task<ActionResult<QueryResponse>> QueryLogAnalytics([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            return BadRequest(new QueryResponse 
            { 
                Success = false, 
                ErrorMessage = "TableName is required" 
            });
        }

        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
        {
            return BadRequest(new QueryResponse 
            { 
                Success = false, 
                ErrorMessage = "WorkspaceId is required" 
            });
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest(new QueryResponse 
            { 
                Success = false, 
                ErrorMessage = "StartTime must be earlier than EndTime" 
            });
        }

        _logger.LogInformation("Received query request for table: {TableName}, workspace: {WorkspaceId}", 
            request.TableName, request.WorkspaceId);

        var result = await _logAnalyticsService.QueryLogAnalyticsAsync(request);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}
