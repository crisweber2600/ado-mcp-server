using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http;
using System;

namespace Ado.Mcp.Tests;

public class HealthTests
{
    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var port = GetFreeTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}";
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", baseUrl);
    Environment.SetEnvironmentVariable("MCP_MINIMAL", "1");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var app = Ado.Mcp.Program.BuildApp(Array.Empty<string>());
    await app.StartAsync(cts.Token);

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        HttpResponseMessage? resp = null;
        for (var i = 0; i < 30; i++)
        {
            try
            {
                resp = await http.GetAsync("/health", cts.Token);
                if (resp.IsSuccessStatusCode) break;
            }
            catch
            {
                await Task.Delay(100, cts.Token);
            }
        }
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp!.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken: cts.Token);
        Assert.NotNull(json);
        Assert.Equal("ok", json!.status);

    await app.StopAsync(cts.Token);
    }

    private record HealthResponse(string status);

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
