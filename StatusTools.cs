using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Ado.Mcp.Tools
{
    /// <summary>
    /// Example placeholder for project status roll-ups. This tool could query Azure DevOps via WIQL and provide aggregated counts.
    /// </summary>
    [McpServerToolType]
    public static class StatusTools
    {
        [McpServerTool, Description("Return a simple project status summary (placeholder implementation)")]
        public static async Task<object> project_status(AdoClient ado, CancellationToken ct = default)
        {
            // This stub simply returns an empty summary. Users can extend this method to compute rollups via WIQL.
            return await Task.FromResult(new
            {
                summary = "Status tool not yet implemented."
            });
        }
    }
}