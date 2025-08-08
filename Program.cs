using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#pragma warning disable IDE0005 // Using directive is unnecessary but included for clarity
using Ado.Mcp.ServiceDefaults.Extensions;
#if INCLUDE_MCP
using ModelContextProtocol.Server;
#endif

namespace Ado.Mcp
{
    /// <summary>
    /// Entry point for the MCP server. This version hosts the MCP server over HTTP instead
    /// of stdio so that both ChatGPT and Copilot Studio can connect via HTTP.
    /// </summary>
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            var app = BuildApp(args);
            await app.RunAsync();
        }

        // Build the WebApplication so tests can host it with TestServer.
    public static WebApplication BuildApp(string[]? args = null, bool useTestServer = false)
        {
            var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

            // Configure logging to write to stderr to align with MCP recommendations.
            builder.Logging.AddConsole(o =>
            {
                o.LogToStandardErrorThreshold = LogLevel.Information;
            });

            // Register HttpClient for AdoClient to call Azure DevOps.
            builder.Services.AddHttpClient<AdoClient>();

            // Allow a minimal mode for smoke tests, and only include MCP in Release builds.
            var minimal = string.Equals(Environment.GetEnvironmentVariable("MCP_MINIMAL"), "1", StringComparison.Ordinal);
#if INCLUDE_MCP
            if (!minimal)
            {
                // Register and configure the MCP server with HTTP transport and discover tools in this assembly.
                builder.Services.AddMcpServer()
                    .WithHttpTransport()
                    .WithToolsFromAssembly();
            }
#endif

            // Hook in service defaults (telemetry/health placeholders via ServiceDefaults library)
            builder.AddServiceDefaults();

            var app = builder.Build();

            // Map MCP endpoints unless in minimal mode.
#if INCLUDE_MCP
            if (!minimal)
            {
                app.MapMcp();
            }
#endif

            // Lightweight health endpoint for smoke tests and readiness checks.
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            app.UseServiceDefaults();

            return app;
        }
    }
}
