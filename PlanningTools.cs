using System.ComponentModel;
using System.Text.Json;
#if INCLUDE_MCP
using ModelContextProtocol.Server;
#endif
using Ado.Mcp.Models;

namespace Ado.Mcp.Tools
{
using System.Collections.Generic;

    /// <summary>
    /// Provides two-stage planning tools: plan_from_brief proposes a backlog and apply_plan applies it.
    /// </summary>
    #if INCLUDE_MCP
    [McpServerToolType]
    #endif
    public static class PlanningTools
    {
        #if INCLUDE_MCP
        [McpServerTool, Description("From a free-text brief, propose a backlog plan (no side effects). Returns Plan JSON.")]
        #endif
        public static string plan_from_brief(
            [Description("Product brief as free text")] string brief,
            [Description("Default area path")] string? areaPath = null,
            [Description("Default iteration path")] string? iterationPath = null)
        {
            // The server returns a template to be filled by the client/LLM. Keep this tool pure.
            var skeleton = new Plan(areaPath, iterationPath, new List<Epic>());
            return JsonSerializer.Serialize(skeleton);
        }

    #if INCLUDE_MCP
    [McpServerTool, Description("Create a hierarchy from a Plan JSON. Returns success flag.")]
    #endif
        public static async Task<object> apply_plan(AdoClient ado, JsonElement planJson, CancellationToken ct = default)
        {
            var plan = JsonSerializer.Deserialize<Plan>(planJson)!;
            foreach (var epic in plan.epics)
            {
                // Create epic
                var eRes = await ado.CreateAsync("Epic", BuildOps(epic.title, epic.description, plan.areaPath, plan.iterationPath), ct);
                var eId = JsonDocument.Parse(await eRes.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetInt32();

                foreach (var feature in epic.features)
                {
                    var fOps = BuildOps(feature.title, feature.description, plan.areaPath, plan.iterationPath).ToList();
                    fOps.Add(Relation(eId));
                    var fRes = await ado.CreateAsync("Feature", fOps, ct);
                    var fId = JsonDocument.Parse(await fRes.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetInt32();

                    foreach (var story in feature.stories)
                    {
                        var sOps = BuildOps(story.title, story.description, plan.areaPath, plan.iterationPath).ToList();
                        sOps.Add(Relation(fId));
                        var sRes = await ado.CreateAsync("User Story", sOps, ct);
                        var sId = JsonDocument.Parse(await sRes.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetInt32();

                        if (story.acceptanceCriteria is { Count: > 0 })
                        {
                            await ado.UpdateAsync(sId, new[] { new { op="add", path="/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value=string.Join("\n", story.acceptanceCriteria) } }, ct);
                        }

                        if (story.tasks is { Count: > 0 })
                        {
                            foreach (var t in story.tasks)
                            {
                                var tOps = BuildOps(t.title, t.description, plan.areaPath, plan.iterationPath).ToList();
                                tOps.Add(Relation(sId));
                                await ado.CreateAsync("Task", tOps, ct);
                            }
                        }
                    }
                }
            }
            return new { ok = true };

            static IEnumerable<object> BuildOps(string title, string? description, string? area, string? iteration)
            {
                yield return new { op = "add", path = "/fields/System.Title", value = title };
                if (!string.IsNullOrWhiteSpace(description))
                    yield return new { op = "add", path = "/fields/System.Description", value = description };
                if (!string.IsNullOrWhiteSpace(area))
                    yield return new { op = "add", path = "/fields/System.AreaPath", value = area };
                if (!string.IsNullOrWhiteSpace(iteration))
                    yield return new { op = "add", path = "/fields/System.IterationPath", value = iteration };
            }

            static object Relation(int id)
            {
                var org = Environment.GetEnvironmentVariable("ADO_ORG");
                var project = Environment.GetEnvironmentVariable("ADO_PROJECT");
                return new
                {
                    op = "add",
                    path = "/relations/-",
                    value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{id}"
                    }
                };
            }
        }
    }
}
