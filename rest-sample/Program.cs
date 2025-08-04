using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LogAnalyticsQueryDemo
{
    public class MetricData
    {
        public DateTime TimeGenerated { get; set; }
        public string MetricName { get; set; } = "";
        public double MetricValue { get; set; }
        public string MetricUnit { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string ServiceVersion { get; set; } = "";
        public string Operation { get; set; } = "";
        public string StatusCode { get; set; } = "";
        public string Properties { get; set; } = "";
        public string TenantId { get; set; } = "";
    }

    public class Program
    {
        // Configuration - Updated with the more specific API endpoint
        private static readonly string SubscriptionId = "7f2413b7-93b1-4560-a932-220c34c9db29";
        private static readonly string ResourceGroup = "rgp-flexenvironment";
        private static readonly string WorkspaceName = "log-ylkxx2tivzs2k";
        private static string AccessToken = "";
        
        // Log Analytics Query API endpoint - Using the more specific endpoint
        private static readonly string QueryEndpoint = $"https://api.loganalytics.io/v1/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/microsoft.operationalinsights/workspaces/{WorkspaceName}/query";
        
        private static readonly HttpClient httpClient = new HttpClient();
        private static DateTime lastUpdateTime = DateTime.UtcNow;
        
        // Store current metrics from latest API response
        private static List<MetricData> currentMetrics = new List<MetricData>();
        
        // Track seen events and their first detection time for latency calculation
        private static Dictionary<string, DateTime> seenEvents = new Dictionary<string, DateTime>();
        private static List<double> latencyMeasurements = new List<double>();
        
        public static async Task Main(string[] args)
        {
            // ASCII Art Header
            Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════════╗
║                                                                      ║
║    ██████╗ ████████╗███████╗██╗      ███████╗███╗   ███╗███████╗     ║
║   ██╔═══██╗╚══██╔══╝██╔════╝██║      ██╔════╝████╗ ████║██╔════╝     ║
║   ██║   ██║   ██║   █████╗  ██║      █████╗  ██╔████╔██║█████╗       ║
║   ██║   ██║   ██║   ██╔══╝  ██║      ██╔══╝  ██║╚██╔╝██║██╔══╝       ║
║   ╚██████╔╝   ██║   ███████╗███████╗ ███████╗██║ ╚═╝ ██║███████╗     ║
║    ╚═════╝    ╚═╝   ╚══════╝╚══════╝ ╚══════╝╚═╝     ╚═╝╚══════╝     ║
║                                                                      ║
║           ████████╗███████╗██╗     ███████╗███╗   ███╗               ║
║           ╚══██╔══╝██╔════╝██║     ██╔════╝████╗ ████║               ║
║              ██║   █████╗  ██║     █████╗  ██╔████╔██║               ║
║              ██║   ██╔══╝  ██║     ██╔══╝  ██║╚██╔╝██║               ║
║              ██║   ███████╗███████╗███████╗██║ ╚═╝ ██║               ║
║              ╚═╝   ╚══════╝╚══════╝╚══════╝╚═╝     ╚═╝               ║
║                                                                      ║
║                  🔍 REAL-TIME METRICS DASHBOARD 🔍                   ║
╚══════════════════════════════════════════════════════════════════════╝
");

            Console.WriteLine("Please enter your Azure Log Analytics Access Token:");
            Console.Write("Token: ");
            AccessToken = Console.ReadLine() ?? "";
            
            if (string.IsNullOrWhiteSpace(AccessToken))
            {
                Console.WriteLine("❌ Access token is required!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Set up HTTP client
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");
            
            // Add headers from Azure portal to get fresh data (not cached)
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Prefer", "wait=600, ai.include-statistics=true, ai.include-render=true, include-datasources=true");
            httpClient.DefaultRequestHeaders.Add("x-ms-app", "AppAnalytics");
            httpClient.DefaultRequestHeaders.Add("x-ms-client-request-info", "Query");

            // Clear console and start dashboard
            Console.Clear();
            
            try
            {
                await StartDashboard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static async Task StartDashboard()
        {
            Console.CursorVisible = false;


            while (true)
            {
                try
                {
                    await QueryLogAnalytics();

                    DisplayDashboard();
                    await Task.Delay(1000); // Query every second
                }
                catch (Exception ex)
                {
                    Console.SetCursorPosition(0, Console.WindowHeight - 3);
                    Console.WriteLine($"❌ Error: {ex.Message}".PadRight(Console.WindowWidth - 1));
                    await Task.Delay(5000); // Wait 5 seconds on error
                }
            }
        }

        private static async Task QueryLogAnalytics()
        {
            var kqlQuery = @"
                OpenTelemetryMetrics_CL
                | where TimeGenerated >= ago(30m)
                | where MetricName in ('http_requests_total', 'http_request_duration_seconds')
                | project TimeGenerated, MetricName, MetricValue, MetricUnit, ServiceName, ServiceVersion, Operation, StatusCode, Properties, TenantId
                | order by TimeGenerated desc
                | limit 1000
            ";

            var requestPayload = new { query = kqlQuery.Trim() };
            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add unique request ID for each call (like Azure portal does)
            var requestId = Guid.NewGuid().ToString();
            if (httpClient.DefaultRequestHeaders.Contains("x-ms-client-request-id"))
                httpClient.DefaultRequestHeaders.Remove("x-ms-client-request-id");
            httpClient.DefaultRequestHeaders.Add("x-ms-client-request-id", requestId);

            var response = await httpClient.PostAsync(QueryEndpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                ProcessMetricsData(responseContent);
                lastUpdateTime = DateTime.UtcNow;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
            }
        }

        private static void ProcessMetricsData(string jsonResponse)
        {
            try
            {
                // Clear current metrics and load fresh data from API response
                currentMetrics.Clear();
                
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                if (root.TryGetProperty("tables", out var tables) && tables.GetArrayLength() > 0)
                {
                    var table = tables[0];
                    
                    if (table.TryGetProperty("columns", out var columns) && table.TryGetProperty("rows", out var rows))
                    {
                        var columnNames = columns.EnumerateArray()
                            .Select(col => col.GetProperty("name").GetString())
                            .ToArray();

                        foreach (var row in rows.EnumerateArray())
                        {
                            var rowData = row.EnumerateArray().ToArray();
                            
                            try
                            {
                                var metric = new MetricData
                                {
                                    TimeGenerated = DateTime.Parse(rowData[0].GetString() ?? "").ToUniversalTime(),
                                    MetricName = rowData[1].GetString() ?? "",
                                    MetricValue = rowData[2].GetDouble(),
                                    MetricUnit = rowData[3].GetString() ?? "",
                                    ServiceName = rowData[4].GetString() ?? "",
                                    ServiceVersion = rowData[5].GetString() ?? "",
                                    Operation = rowData[6].GetString() ?? "",
                                    StatusCode = rowData[7].GetInt32().ToString(), // StatusCode is an integer
                                    Properties = rowData[8].GetString() ?? "",
                                    TenantId = rowData[9].GetString() ?? ""
                                };

                                // Add metric to current metrics list
                                currentMetrics.Add(metric);
                                
                                // Track first-time seen events for latency calculation
                                var eventKey = $"{metric.TimeGenerated:yyyy-MM-dd HH:mm:ss.fff}_{metric.MetricName}_{metric.Operation}_{metric.StatusCode}";
                                if (!seenEvents.ContainsKey(eventKey))
                                {
                                    var firstSeenTime = DateTime.UtcNow;
                                    seenEvents[eventKey] = firstSeenTime;
                                    
                                    // Calculate latency for this first-time seen event
                                    var latency = (firstSeenTime - metric.TimeGenerated).TotalSeconds;
                                    if (latency >= 0) // Only add positive latencies
                                    {
                                        latencyMeasurements.Add(latency);
                                        
                                        // Keep only last 1000 measurements to prevent memory growth
                                        if (latencyMeasurements.Count > 1000)
                                        {
                                            latencyMeasurements.RemoveAt(0);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Silently continue processing on row parsing errors
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently handle JSON parsing errors
            }
        }

        private static void DisplayDashboard()
        {
            Console.Clear();
            
            // Header
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
+==============================================================================+
|                    OPENTELEMETRY METRICS DASHBOARD                          |
+==============================================================================+");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Last Updated: {lastUpdateTime:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Auto-refresh every 1 second | Current metrics: {currentMetrics.Count}");
            Console.ResetColor();
            Console.WriteLine();

            // Display current data from sliding window
            DisplayMetricsData();

            // Footer
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("=".PadRight(Console.WindowWidth - 1, '='));
            Console.WriteLine($"Workspace: {WorkspaceName} | Next update in 1 second | Press Ctrl+C to exit");
            Console.ResetColor();
        }

        private static void DisplayMetricsData()
        {
            if (!currentMetrics.Any())
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("  Waiting for metrics data...");
                Console.ResetColor();
                return;
            }

            // Display total request count using ASCII art
            DisplayRequestCountGraph();

            Console.WriteLine();

            // Summary of current response
            DisplaySummary();
        }

        private static void DisplayRequestCountGraph()
        {
            var requestEvents = currentMetrics.Where(e => e.MetricName == "http_requests_total").ToArray();
            var totalRequests = requestEvents.Sum(e => e.MetricValue);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
+==============================================================================+
|                               TOTAL REQUESTS                                |
+==============================================================================+");
            Console.ResetColor();
            Console.WriteLine();

            // Display large ASCII art numbers for total requests
            DisplayLargeNumber((int)totalRequests);
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"                            Total HTTP Requests");
            Console.ResetColor();
        }

        private static void DisplayLargeNumber(int number)
        {
            var numberStr = number.ToString();
            var digitLines = new string[7]; // 7 lines high for ASCII art
            
            // Initialize lines
            for (int i = 0; i < 7; i++)
                digitLines[i] = "    "; // Leading spaces for centering
            
            foreach (char digit in numberStr)
            {
                var digitArt = GetDigitArt(digit);
                for (int i = 0; i < 7; i++)
                {
                    digitLines[i] += digitArt[i] + "  "; // Add spacing between digits
                }
            }
            
            // Display the large number with color
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var line in digitLines)
            {
                Console.WriteLine($"            {line}"); // Center alignment
            }
            Console.ResetColor();
        }

        private static string[] GetDigitArt(char digit)
        {
            return digit switch
            {
                '0' => new string[]
                {
                    "███████",
                    "██   ██",
                    "██   ██",
                    "██   ██",
                    "██   ██",
                    "██   ██",
                    "███████"
                },
                '1' => new string[]
                {
                    "   ██  ",
                    "  ███  ",
                    "   ██  ",
                    "   ██  ",
                    "   ██  ",
                    "   ██  ",
                    "███████"
                },
                '2' => new string[]
                {
                    "███████",
                    "     ██",
                    "     ██",
                    "███████",
                    "██     ",
                    "██     ",
                    "███████"
                },
                '3' => new string[]
                {
                    "███████",
                    "     ██",
                    "     ██",
                    "███████",
                    "     ██",
                    "     ██",
                    "███████"
                },
                '4' => new string[]
                {
                    "██   ██",
                    "██   ██",
                    "██   ██",
                    "███████",
                    "     ██",
                    "     ██",
                    "     ██"
                },
                '5' => new string[]
                {
                    "███████",
                    "██     ",
                    "██     ",
                    "███████",
                    "     ██",
                    "     ██",
                    "███████"
                },
                '6' => new string[]
                {
                    "███████",
                    "██     ",
                    "██     ",
                    "███████",
                    "██   ██",
                    "██   ██",
                    "███████"
                },
                '7' => new string[]
                {
                    "███████",
                    "     ██",
                    "     ██",
                    "     ██",
                    "     ██",
                    "     ██",
                    "     ██"
                },
                '8' => new string[]
                {
                    "███████",
                    "██   ██",
                    "██   ██",
                    "███████",
                    "██   ██",
                    "██   ██",
                    "███████"
                },
                '9' => new string[]
                {
                    "███████",
                    "██   ██",
                    "██   ██",
                    "███████",
                    "     ██",
                    "     ██",
                    "███████"
                },
                _ => new string[]
                {
                    "       ",
                    "       ",
                    "       ",
                    "       ",
                    "       ",
                    "       ",
                    "       "
                }
            };
        }

        private static void DisplaySummary()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
+==============================================================================+
|                              CURRENT SUMMARY                                |
+==============================================================================+");
            Console.ResetColor();

            var requestEvents = currentMetrics.Where(e => e.MetricName == "http_requests_total");
            var durationEvents = currentMetrics.Where(e => e.MetricName == "http_request_duration_seconds");
            var totalRequests = requestEvents.Sum(e => e.MetricValue);
            var avgDuration = durationEvents.Any() ? durationEvents.Average(e => e.MetricValue) * 1000 : 0;

            // Calculate average log latency (how long events take to appear in console)
            // Use stored latency measurements from first-time seen events
            var avgLogLatency = latencyMeasurements.Any() ? latencyMeasurements.Average() : 0;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Request Metrics: {requestEvents.Count()}");
            Console.WriteLine($"Duration Metrics: {durationEvents.Count()}");
            Console.WriteLine($"Total Requests: {totalRequests:N0}");
            Console.WriteLine($"Average API Execution Duration: {avgDuration:F2} ms");
            Console.WriteLine($"Average Log Latency: {avgLogLatency:F1} seconds ({latencyMeasurements.Count} samples)");
            if (currentMetrics.Any())
            {
                var oldestMetric = currentMetrics.Min(e => e.TimeGenerated);
                var newestMetric = currentMetrics.Max(e => e.TimeGenerated);
                var timespan = newestMetric - oldestMetric;
                Console.WriteLine($"Data Span: {timespan.TotalMinutes:F1} minutes");
                Console.WriteLine($"Oldest Metric: {oldestMetric:HH:mm:ss}");
                Console.WriteLine($"Newest Metric: {newestMetric:HH:mm:ss}");
            }
            Console.ResetColor();
        }

        private static void DisplayTimelineGraph()
        {
            // This method is no longer used, replaced by DisplayRequestCountGraph
            // Keeping for potential future use
        }
    }
}
