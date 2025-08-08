using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Ado.Mcp
{
    /// <summary>
    /// Entry point for the MCP server. This version hosts the MCP server over HTTP instead
    /// of stdio so that both ChatGPT and Copilot Studio can connect via HTTP.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Use the ASP.NET Core WebApplication builder to set up an HTTP server.
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging to write to stderr to align with MCP recommendations.
            builder.Logging.AddConsole(o =>
            {
                o.LogToStandardErrorThreshold = LogLevel.Information;
            });

            // Register HttpClient for AdoClient to call Azure DevOps.
            builder.Services.AddHttpClient<AdoClient>();

            // Register and configure the MCP server with HTTP transport and discover tools in this assembly.
            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly();

            var app = builder.Build();

            // Map MCP endpoints so that clients can discover and call the tools over HTTP.
            app.MapMcp();

            // Start the HTTP server. By default, this will listen on the configured URLs (e.g. http://localhost:5000).
            await app.RunAsync();
        }
    }
}
