using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ado.Mcp
{
    /// <summary>
    /// Thin wrapper around the Azure DevOps REST API for work item operations.
    /// </summary>
    public sealed class AdoClient
    {
        private readonly HttpClient _http;
        private readonly string _org;
        private readonly string _project;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public AdoClient(HttpClient http)
        {
            _http = http;
            _org = Environment.GetEnvironmentVariable("ADO_ORG") ?? throw new InvalidOperationException("ADO_ORG missing");
            _project = Environment.GetEnvironmentVariable("ADO_PROJECT") ?? throw new InvalidOperationException("ADO_PROJECT missing");
            var pat = Environment.GetEnvironmentVariable("ADO_PAT") ?? throw new InvalidOperationException("ADO_PAT missing");
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            _http.BaseAddress = new Uri($"https://dev.azure.com/{_org}/");
        }

        public Task<HttpResponseMessage> WiqlAsync(string wiql, CancellationToken ct) =>
            _http.PostAsync($"{_project}/_apis/wit/wiql?api-version=7.1",
                new StringContent(JsonSerializer.Serialize(new { query = wiql }), Encoding.UTF8, "application/json"), ct);

        public Task<HttpResponseMessage> WorkItemsBatchAsync(object body, CancellationToken ct) =>
            _http.PostAsync($"{_project}/_apis/wit/workitemsbatch?api-version=7.1",
                new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json"), ct);

        public Task<HttpResponseMessage> GetWorkItemAsync(int id, string expand, CancellationToken ct) =>
            _http.GetAsync($"{_project}/_apis/wit/workitems/{id}?$expand={expand}&api-version=7.1", ct);

        public Task<HttpResponseMessage> CreateAsync(string type, IEnumerable<object> ops, CancellationToken ct) =>
            _http.PostAsync($"{_project}/_apis/wit/workitems/${Uri.EscapeDataString(type)}?api-version=7.1",
                new StringContent(JsonSerializer.Serialize(ops, JsonOpts), Encoding.UTF8, "application/json-patch+json"), ct);

        public Task<HttpResponseMessage> UpdateAsync(int id, IEnumerable<object> ops, CancellationToken ct) =>
            _http.PatchAsync($"{_project}/_apis/wit/workitems/{id}?api-version=7.1",
                new StringContent(JsonSerializer.Serialize(ops, JsonOpts), Encoding.UTF8, "application/json-patch+json"), ct);

        public Task<HttpResponseMessage> AddCommentAsync(int id, string text, CancellationToken ct) =>
            _http.PostAsync($"{_project}/_apis/wit/workItems/{id}/comments?api-version=7.1-preview.4",
                new StringContent(JsonSerializer.Serialize(new { text }), Encoding.UTF8, "application/json"), ct);

        public Task<HttpResponseMessage> UploadAttachmentAsync(Stream stream, string fileName, CancellationToken ct) =>
            _http.PostAsync($"_apis/wit/attachments?fileName={Uri.EscapeDataString(fileName)}&api-version=7.1",
                new StreamContent(stream) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } }, ct);
    }
}