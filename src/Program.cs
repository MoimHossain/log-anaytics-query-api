using Azure.Identity;
using Azure.Monitor.Query;
using LogAnalyticsQueryApi.Models;
using LogAnalyticsQueryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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

// Register Azure Monitor Query service
builder.Services.AddSingleton<LogsQueryClient>(provider =>
{
    var credential = new DefaultAzureCredential();
    return new LogsQueryClient(credential);
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

app.UseAuthorization();

app.MapControllers();

app.Run();
