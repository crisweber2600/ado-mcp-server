# Copilot instructions for ado-mcp-server

Purpose: Help AI coding agents be productive in this repo by capturing the architecture, workflows, and project-specific patterns.

## Big picture
- This is an ASP.NET Core minimal app that hosts an MCP (Model Context Protocol) server over HTTP to wrap Azure DevOps Work Item APIs.
- Two build modes:
  - Debug: minimal runtime; MCP packages excluded; health endpoint only. Toggle minimal mode with env MCP_MINIMAL=1.
  - Release: defines INCLUDE_MCP, adds MCP packages, discovers and maps MCP tools over HTTP.
- Entry: `Program.BuildApp(args, useTestServer=false)` returns `WebApplication` for tests; `Main` calls `RunAsync`.
- Health probe: `GET /health` returns `{ status: "ok" }` in all modes.

## Key files
- `Program.cs` – configures logging to stderr, registers `HttpClient<AdoClient>`, wires MCP in Release: `AddMcpServer().WithHttpTransport().WithToolsFromAssembly(); app.MapMcp();`
- `AdoClient.cs` – thin Azure DevOps REST wrapper using env vars `ADO_ORG`, `ADO_PROJECT`, `ADO_PAT` (PAT required: vso.work or vso.work_write). Uses `System.Text.Json` with Web defaults.
- `Ado.Mcp.Tools/*`:
  - `AzureDevOpsTools.cs` – MCP tool surface for work items: `get_work_item`, `query_wiql`, `get_work_items_batch`, `create_work_item`, `update_work_item` (JSON Patch), `transition_state`, `add_comment`.
  - `PlanningTools.cs` – two-stage backlog planning: `plan_from_brief` (pure; returns Plan JSON skeleton) and `apply_plan` (creates Epic → Feature → User Story → Task hierarchy, links using `System.LinkTypes.Hierarchy-Reverse`, sets fields including `Microsoft.VSTS.Common.AcceptanceCriteria`).
  - `StatusTools.cs` – stub example (`project_status`).
- `PlanModels.cs` – DTOs: `Plan`, `Epic`, `Feature`, `Story`, `TaskItem`.
- `Ado.Mcp.csproj` – only in Release: includes `ModelContextProtocol*` packages and defines `INCLUDE_MCP`.
- `mcp.json` / `server.json` – example client config and package metadata; document required env vars for MCP clients.
- `Ado.Mcp.Tests/HealthTests.cs` – smoke test using `BuildApp`, sets `ASPNETCORE_URLS` and `MCP_MINIMAL=1`, asserts `/health`.

## Conventions & patterns
- DI-first: tools are `static` methods that accept `AdoClient ado` so DI can inject the configured client.
- JSON: use `System.Text.Json`; for JSON Patch send `application/json-patch+json` and arrays of ops like `{ op, path, value }`.
- Azure DevOps specifics:
  - API version `7.1`; base `https://dev.azure.com/{ADO_ORG}/`.
  - Work item create types: `Epic`, `Feature`, `User Story`, `Task`.
  - Link parent-child with relation `System.LinkTypes.Hierarchy-Reverse` and URL to the parent work item.
  - Acceptance Criteria field: `Microsoft.VSTS.Common.AcceptanceCriteria`.
- MCP wiring is conditional: wrap attributes with `#if INCLUDE_MCP` and use `[McpServerToolType]` on classes and `[McpServerTool]` on methods.

## Developer workflows (PowerShell)
- Build (Debug minimal):
  - `dotnet restore .\ado-mcp-server.sln`
  - `dotnet build .\ado-mcp-server.sln -c Debug`
- Run locally (Debug):
  - Set `ASPNETCORE_URLS` (e.g., `http://localhost:5088`), optionally `MCP_MINIMAL=1` to skip MCP endpoints.
  - `dotnet run --project .\Ado.Mcp.csproj`
- Tests:
  - `dotnet test .\ado-mcp-server.sln -c Debug`
- Release with full MCP:
  - `dotnet build .\ado-mcp-server.sln -c Release` (enables `INCLUDE_MCP`, maps `/mcp`).

## Adding a new tool (example checklist)
- Create a public `static` method in a class under `Ado.Mcp.Tools` that takes `AdoClient ado` as the first param; keep shapes simple (primitives or `JsonElement`).
- Behind `#if INCLUDE_MCP`, annotate the class with `[McpServerToolType]` and the method with `[McpServerTool]` and an informative `[Description]`.
- Reuse helpers from `AdoClient` and mirror patterns in `AzureDevOpsTools` (JSON Patch, WIQL, batch).
- If adding new ADO fields/relations, follow the naming and relation URL patterns in `PlanningTools.apply_plan`.

## Useful examples
- Batch fetch with fields: `get_work_items_batch(ids: [1,2], fields: ["System.Title"], expand: "Relations")`.
- Transition a bug: `transition_state(id: 123, newState: "Closed", reason: "Fixed")`.
- Create a story under a feature: `create_work_item(type: "User Story", title: "Do X", parentId: <featureId>, areaPath: "Team Area", iterationPath: "Project\Sprint 1")`.

## Gotchas
- Missing env vars (`ADO_ORG`, `ADO_PROJECT`, `ADO_PAT`) throw at `AdoClient` construction time.
- MCP endpoints are only mapped in Release AND when `MCP_MINIMAL` is not `1`.
- When building JSON payloads that need `$expand`, use a dictionary (see `get_work_items_batch`).
