using Ado.Mcp;
using Ado.Mcp.ServiceDefaults.Extensions;

// This AppHost orchestrates the Ado.Mcp service similar to .NET Aspire patterns.
// It configures base URLs and minimal env to run the service.

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5088";
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", urls);
// Default to minimal mode so MCP endpoints are not required when running locally
// You can set MCP_MINIMAL=0 to enable MCP endpoints in Release builds.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MCP_MINIMAL")))
{
    Environment.SetEnvironmentVariable("MCP_MINIMAL", "1");
}

var app = Ado.Mcp.Program.BuildApp(Array.Empty<string>());

app.UseServiceDefaults();

await app.RunAsync();
