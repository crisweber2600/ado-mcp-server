# Ado MCP Server

Simple MCP-over-HTTP server that wraps Azure DevOps work item APIs. This repo now includes a minimal smoke test to validate the process can start and respond to `/health`.

## Prereqs
- .NET 8 SDK
- Windows PowerShell (commands below assume PowerShell)

## Build
```
# from repo root
 dotnet restore .\ado-mcp-server.sln
 dotnet build .\ado-mcp-server.sln -c Debug
```

## Run
```
# optional: override port
 $env:ASPNETCORE_URLS = "http://localhost:5088"
 dotnet run --project .\Ado.Mcp.csproj
```
Then open http://localhost:5088/health and expect `{ "status": "ok" }`.

### Run via AppHost (Aspire-style)
```
# Orchestrates the service with ServiceDefaults
dotnet run --project .\Ado.Mcp.AppHost\Ado.Mcp.AppHost.csproj
```
By default, `MCP_MINIMAL=1` is set in AppHost. Set `MCP_MINIMAL=0` and build Release to enable MCP endpoints.

## Smoke tests
By default, Debug builds exclude heavy MCP preview packages to keep tests fast and resilient. Release builds include full MCP features.

Run the smoke tests:
```
 dotnet test .\ado-mcp-server.sln -c Debug
```
This starts the app on a random port with `MCP_MINIMAL=1` and hits `/health`.

## Release build with MCP
```
 dotnet build .\ado-mcp-server.sln -c Release
```
This enables the `INCLUDE_MCP` define, includes MCP packages, and maps MCP endpoints.
