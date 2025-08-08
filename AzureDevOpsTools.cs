using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Ado.Mcp.Tools
{
    /// <summary>
    /// Set of MCP tools that wrap common Azure DevOps work item operations.
    /// </summary>
    [McpServerToolType]
    public static class AzureDevOpsTools
    {
        [McpServerTool, Description("Get a work item by ID")]
        public static async Task<string> get_work_item(AdoClient ado, int id, string expand = "Relations", CancellationToken ct = default)
            => await (await ado.GetWorkItemAsync(id, expand, ct)).Content.ReadAsStringAsync(ct);

        [McpServerTool, Description("Run a WIQL query and return work item refs")]
        public static async Task<string> query_wiql(AdoClient ado, string wiql, CancellationToken ct = default)
            => await (await ado.WiqlAsync(wiql, ct)).Content.ReadAsStringAsync(ct);

        [McpServerTool, Description("Batch get work items (max 200)")]
        public static async Task<string> get_work_items_batch(AdoClient ado, int[] ids, string[]? fields = null, string expand = "Relations", CancellationToken ct = default)
        {
            var body = new { ids, fields, $expand = expand };
            return await (await ado.WorkItemsBatchAsync(body, ct)).Content.ReadAsStringAsync(ct);
        }

        [McpServerTool, Description("Create a work item of the given type ($User Story, $Feature, $Task, etc.)")]
        public static async Task<string> create_work_item(AdoClient ado, string type, string title, string? description = null, int? parentId = null, string? areaPath = null, string? iterationPath = null, CancellationToken ct = default)
        {
            var ops = new List<object> {
                new { op="add", path="/fields/System.Title", value=title }
            };
            if (!string.IsNullOrWhiteSpace(description))
                ops.Add(new { op="add", path="/fields/System.Description", value=description });
            if (!string.IsNullOrWhiteSpace(areaPath))
                ops.Add(new { op="add", path="/fields/System.AreaPath", value=areaPath });
            if (!string.IsNullOrWhiteSpace(iterationPath))
                ops.Add(new { op="add", path="/fields/System.IterationPath", value=iterationPath });
            if (parentId is int pid)
            {
                var org = Environment.GetEnvironmentVariable("ADO_ORG");
                var project = Environment.GetEnvironmentVariable("ADO_PROJECT");
                ops.Add(new { op="add", path="/relations/-", value = new { rel="System.LinkTypes.Hierarchy-Reverse", url=$"https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{pid}" } });
            }
            return await (await ado.CreateAsync(type, ops, ct)).Content.ReadAsStringAsync(ct);
        }

        [McpServerTool, Description("Update fields on a work item (JSON Patch operations)")]
        public static async Task<string> update_work_item(AdoClient ado, int id, JsonElement patch, CancellationToken ct = default)
        {
            var list = JsonSerializer.Deserialize<List<object>>(patch.GetRawText()) ?? new();
            return await (await ado.UpdateAsync(id, list, ct)).Content.ReadAsStringAsync(ct);
        }

        [McpServerTool, Description("Transition a work item to a new state")]
        public static Task<string> transition_state(AdoClient ado, int id, string newState, string? reason = null, CancellationToken ct = default)
        {
            var ops = new List<object> {
                new { op="add", path="/fields/System.State", value = newState }
            };
            if (!string.IsNullOrWhiteSpace(reason))
                ops.Add(new { op="add", path="/fields/System.Reason", value = reason });
            var json = JsonSerializer.Serialize(ops);
            return update_work_item(ado, id, JsonDocument.Parse(json).RootElement, ct);
        }

        [McpServerTool, Description("Add a comment to a work item")]
        public static async Task<string> add_comment(AdoClient ado, int id, string text, CancellationToken ct = default)
            => await (await ado.AddCommentAsync(id, text, ct)).Content.ReadAsStringAsync(ct);
    }
}