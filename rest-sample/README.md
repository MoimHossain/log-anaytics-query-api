# Azure Log Analytics Query Demo

This is a simple C# console application that demonstrates how to query Azure Log Analytics using direct HTTP requests without any SDK.

## Features

- Queries the `OpenTelemetryMetrics_CL` table for the last 7 days
- Uses raw HTTP requests to demonstrate the Log Analytics REST API structure
- Displays results in a formatted table
- Includes error handling and helpful debugging information

## Setup

1. **Get your Log Analytics Workspace ID:**
   - Go to Azure Portal → Log Analytics workspaces
   - Select your workspace
   - Copy the "Workspace ID" from the Overview page

2. **Get an Access Token:**
   
   **Option A: Using Azure CLI (Recommended for testing)**
   ```bash
   az login
   az account get-access-token --resource https://api.loganalytics.io
   ```
   
   **Option B: Using Azure PowerShell**
   ```powershell
   Connect-AzAccount
   $context = Get-AzContext
   [Microsoft.Azure.Commands.Common.Authentication.AzureSession]::Instance.AuthenticationFactory.Authenticate($context.Account, $context.Environment, $context.Tenant.Id, $null, "https://api.loganalytics.io/.default", $null).AccessToken
   ```

3. **Update the Program.cs file:**
   - Replace `YOUR_WORKSPACE_ID` with your actual workspace ID
   - Replace `YOUR_ACCESS_TOKEN` with the token from step 2

## Running the Application

```bash
cd c:\GitHub\moimhossain\log-anaytics-query-api\rest-sample
dotnet run
```

## API Details

The program makes a POST request to:
```
https://api.loganalytics.io/v1/workspaces/{workspaceId}/query
```

With the following headers:
- `Authorization: Bearer {accessToken}`
- `Content-Type: application/json`

And a JSON payload containing the KQL query:
```json
{
  "query": "OpenTelemetryMetrics_CL | where TimeGenerated >= ago(7d) | order by TimeGenerated desc | limit 100"
}
```

## Permissions Required

Your account needs:
- **Log Analytics Reader** role on the Log Analytics workspace
- **Reader** role on the resource group (for workspace access)

## Troubleshooting

### 401 Unauthorized
- Token expired: Get a new access token
- Wrong scope: Ensure token has `https://api.loganalytics.io/Data.Read` scope

### 403 Forbidden
- Missing permissions: Add "Log Analytics Reader" role
- Wrong workspace ID: Verify the workspace ID is correct

### No Data Returned
- Table doesn't exist: Check if `OpenTelemetryMetrics_CL` exists in your workspace
- No recent data: Adjust the time range in the query
- Different table name: Verify the exact table name

## Customizing the Query

You can modify the KQL query in the `QueryLogAnalytics()` method. For example:

```csharp
var kqlQuery = @"
    OpenTelemetryMetrics_CL
    | where TimeGenerated >= ago(1d)
    | where MetricName_s == 'your_metric_name'
    | summarize avg(MetricValue_d) by bin(TimeGenerated, 1h)
    | order by TimeGenerated desc
";
```

## Alternative Authentication Methods

For production scenarios, consider:
- **Managed Identity**: If running on Azure resources
- **Service Principal**: For automated scenarios
- **Azure Key Vault**: For storing credentials securely
