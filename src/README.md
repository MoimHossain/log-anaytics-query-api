# Log Analytics Query API

A simple .NET 8 Web API that allows querying Azure Log Analytics workspaces. This API acts as a proxy client that handles authentication to Log Analytics and executes queries based on table name and time range.

## Features

- Simple REST API for querying Log Analytics
- Azure authentication using DefaultAzureCredential
- CORS enabled for all origins
- Swagger/OpenAPI documentation
- Docker support
- Configurable query parameters (table name, time range, additional filters)

## Prerequisites

- .NET 8 SDK
- Azure credentials configured (via Azure CLI, Managed Identity, or environment variables)
- Access to Azure Log Analytics workspace

## Authentication

This API uses Azure's `DefaultAzureCredential` which automatically tries multiple authentication methods:
1. Environment variables
2. Managed Identity (when running in Azure)
3. Azure CLI credentials
4. Visual Studio credentials
5. Azure PowerShell credentials

Make sure you have appropriate permissions to read from the Log Analytics workspace.

## API Endpoints

### POST /api/loganalytics/query

Query a Log Analytics workspace.

**Request Body:**
```json
{
  "tableName": "AzureActivity",
  "workspaceId": "your-workspace-id",
  "startTime": "2024-01-01T00:00:00Z",
  "endTime": "2024-01-01T23:59:59Z",
  "additionalFilters": "where ResourceGroup == 'myResourceGroup'",
  "top": 100
}
```

**Response:**
```json
{
  "success": true,
  "errorMessage": null,
  "data": [
    {
      "TimeGenerated": "2024-01-01T10:00:00Z",
      "ResourceGroup": "myResourceGroup",
      "OperationName": "Create or Update Virtual Machine",
      // ... other fields
    }
  ],
  "rowCount": 50
}
```

### GET /api/loganalytics/health

Health check endpoint.

## Running the Application

### Local Development

1. Navigate to the src directory:
   ```bash
   cd src
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7000` and `http://localhost:5000`.

### Using Docker

1. Build the Docker image:
   ```bash
   cd src
   docker build -t log-analytics-api .
   ```

2. Run the container:
   ```bash
   docker run -p 8080:8080 \
     -e AZURE_CLIENT_ID=your-client-id \
     -e AZURE_CLIENT_SECRET=your-client-secret \
     -e AZURE_TENANT_ID=your-tenant-id \
     log-analytics-api
   ```

## Configuration

### Environment Variables for Authentication

- `AZURE_CLIENT_ID`: Service principal client ID
- `AZURE_CLIENT_SECRET`: Service principal client secret  
- `AZURE_TENANT_ID`: Azure tenant ID

### Alternative Authentication Methods

When running locally, you can authenticate using:
- Azure CLI: `az login`
- Visual Studio: Sign in through Visual Studio
- Azure PowerShell: `Connect-AzAccount`

## Example Usage

### Using curl

```bash
curl -X POST "http://localhost:5000/api/loganalytics/query" \
  -H "Content-Type: application/json" \
  -d '{
    "tableName": "AzureActivity",
    "workspaceId": "12345678-1234-1234-1234-123456789012",
    "startTime": "2024-01-01T00:00:00Z",
    "endTime": "2024-01-01T23:59:59Z",
    "top": 10
  }'
```

### Using JavaScript/Fetch

```javascript
const response = await fetch('http://localhost:5000/api/loganalytics/query', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    tableName: 'AzureActivity',
    workspaceId: '12345678-1234-1234-1234-123456789012',
    startTime: '2024-01-01T00:00:00Z',
    endTime: '2024-01-01T23:59:59Z',
    top: 10
  })
});

const data = await response.json();
console.log(data);
```

## Security Considerations

- This API allows CORS from all origins for simplicity. In production, restrict CORS to specific domains.
- Ensure proper authentication and authorization are configured in your environment.
- Consider implementing rate limiting and input validation for production use.
- The workspace ID is passed in the request body - ensure proper access controls.

## Common Log Analytics Tables

- `AzureActivity` - Azure activity logs
- `AzureDiagnostics` - Azure diagnostic logs
- `Heartbeat` - VM heartbeat data
- `Perf` - Performance counters
- `Syslog` - Linux syslog data
- `Event` - Windows event logs

## Troubleshooting

1. **Authentication Issues**: Ensure your credentials have the "Log Analytics Reader" role on the workspace.
2. **Workspace Not Found**: Verify the workspace ID is correct.
3. **Query Errors**: Check that the table name exists and your KQL syntax is correct.
4. **CORS Issues**: The API is configured to allow all origins, so CORS shouldn't be an issue.

## API Documentation

When running in development mode, Swagger UI is available at:
- `http://localhost:5000/swagger`
- `https://localhost:7000/swagger`
