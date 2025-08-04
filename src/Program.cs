using Azure.Identity;
using Azure.Monitor.Query;
using LogAnalyticsQueryApi.Models;
using LogAnalyticsQueryApi.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS to allow all origins, headers, and methods
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Azure authentication settings
builder.Services.Configure<AzureAuthConfig>(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? builder.Configuration["AzureAuth:ClientId"] ?? string.Empty;
    options.ClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? builder.Configuration["AzureAuth:ClientSecret"] ?? string.Empty;
    options.TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? builder.Configuration["AzureAuth:TenantId"] ?? string.Empty;
});

// Register Azure Monitor Query service
builder.Services.AddSingleton<LogsQueryClient>(provider =>
{
    //var azureAuthConfig = provider.GetRequiredService<IOptions<AzureAuthConfig>>().Value;
    
    //if (string.IsNullOrWhiteSpace(azureAuthConfig.ClientId) || 
    //    string.IsNullOrWhiteSpace(azureAuthConfig.ClientSecret) || 
    //    string.IsNullOrWhiteSpace(azureAuthConfig.TenantId))
    //{
    //    throw new InvalidOperationException("Azure authentication configuration is incomplete. Please ensure AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID environment variables are set or configured in appsettings.json");
    //}
    
    //var credential = new ClientSecretCredential(
    //    azureAuthConfig.TenantId,
    //    azureAuthConfig.ClientId,
    //    azureAuthConfig.ClientSecret);
    var options = new LogsQueryClientOptions();
    return new LogsQueryClient(new DefaultAzureCredential(), options);
});

builder.Services.AddScoped<ILogAnalyticsService, LogAnalyticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors();

// Minimal API endpoints
app.MapPost("/api/loganalytics/query", async (QueryRequest request, ILogAnalyticsService logAnalyticsService, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.TableName))
    {
        return Results.BadRequest(new QueryResponse 
        { 
            Success = false, 
            ErrorMessage = "TableName is required" 
        });
    }

    if (string.IsNullOrWhiteSpace(request.WorkspaceId))
    {
        return Results.BadRequest(new QueryResponse 
        { 
            Success = false, 
            ErrorMessage = "WorkspaceId is required" 
        });
    }

    if (request.StartTime >= request.EndTime)
    {
        return Results.BadRequest(new QueryResponse 
        { 
            Success = false, 
            ErrorMessage = "StartTime must be earlier than EndTime" 
        });
    }

    logger.LogInformation("Received query request for table: {TableName}, workspace: {WorkspaceId}", 
        request.TableName, request.WorkspaceId);

    var result = await logAnalyticsService.QueryLogAnalyticsAsync(request);

    if (result.Success)
    {
        return Results.Ok(result);
    }
    else
    {
        return Results.Problem(detail: result.ErrorMessage, statusCode: 500);
    }
})
.WithName("QueryLogAnalytics")
.WithOpenApi(operation => new(operation)
{
    Summary = "Query Log Analytics workspace",
    Description = "Execute a query against an Azure Log Analytics workspace with the specified table name and time range"
});

app.MapGet("/api/loganalytics/health", () =>
{
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi(operation => new(operation)
{
    Summary = "Health check endpoint",
    Description = "Returns the health status of the API"
});

app.Run();
