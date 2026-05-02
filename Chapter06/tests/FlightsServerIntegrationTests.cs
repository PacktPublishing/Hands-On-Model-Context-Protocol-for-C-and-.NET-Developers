// Chapter 6 — Integration tests for SearchFlights (Section 6.1.3) and contract tests
// for the live tool descriptors (Section 6.1.4).
//
// The Chapter 5 FlightsServer is HTTP-only (UseUrls("http://0.0.0.0:5002")), so we
// connect over HttpClientTransport. The test harness will:
//   1. Reuse a server already listening on port 5002 if one is available.
//   2. Otherwise launch Chapter 5 as a child process and tear it down on dispose.
//   3. Skip cleanly if neither path produces a reachable server.
//
// Marked [Trait("Execution","Manual")] — skipped from default runs via
// --filter "Execution!=Manual". Build Chapter 5 first:
//   dotnet build ../../Chapter05/code/Chapter05.csproj
// Run only this trait:
//   dotnet test --filter "Execution=Manual"

using ModelContextProtocol.Client;
using System.Diagnostics;
using System.Net.Sockets;
using Xunit;

namespace TravelBooking.Chapter06.Tests;

[Trait("Execution", "Manual")]
public class FlightsServerIntegrationTests : IAsyncLifetime
{
    private const int ServerPort = 5002;
    private static readonly Uri ServerEndpoint = new($"http://localhost:{ServerPort}/mcp");

    private static readonly string Chapter05Project =
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "Chapter05", "code", "Chapter05.csproj"));

    private Process? _spawnedServer;
    private McpClient? _client;

    public async ValueTask InitializeAsync()
    {
        var alreadyListening = await IsPortListeningAsync(ServerPort, TimeSpan.FromMilliseconds(250));
        if (!alreadyListening)
        {
            if (!File.Exists(Chapter05Project))
                Assert.Skip($"Chapter 5 project not found at {Chapter05Project}.");

            _spawnedServer = StartChapter05Server();
            var ready = await WaitForPortAsync(ServerPort, TimeSpan.FromSeconds(30));
            if (!ready)
            {
                TerminateSpawnedServer();
                Assert.Skip(
                    $"Chapter 5 server did not start listening on port {ServerPort} in time. " +
                    "Build it first with: dotnet build ../../Chapter05/code/Chapter05.csproj");
            }
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = "FlightsServer",
            Endpoint = ServerEndpoint,
        });

        _client = await McpClient.CreateAsync(transport,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();

        TerminateSpawnedServer();
    }

    [Fact]
    public async Task SearchFlights_ValidRoute_ReturnsResults()
    {
        var result = await _client!.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR",
                ["destination"] = "AMS",
                ["departureDate"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.IsError);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task AllExpectedTools_AreRegistered()
    {
        var tools = await _client!.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var names = tools.Select(t => t.Name).ToHashSet();
        Assert.Contains("search_flights", names);
        Assert.Contains("book_flight", names);
        Assert.Contains("cancel_flight", names);
    }

    [Fact]
    public async Task AllTools_HaveNonEmptyDescriptions()
    {
        var tools = await _client!.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        foreach (var tool in tools)
            Assert.False(string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' is missing a description.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Process StartChapter05Server()
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { "run", "--no-build", "--project", Chapter05Project },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Chapter 5 server process.");
        return proc;
    }

    private void TerminateSpawnedServer()
    {
        if (_spawnedServer is null) return;
        try
        {
            if (!_spawnedServer.HasExited)
            {
                _spawnedServer.Kill(entireProcessTree: true);
                _spawnedServer.WaitForExit(TimeSpan.FromSeconds(10));
            }
        }
        catch { /* best effort */ }
        finally
        {
            _spawnedServer.Dispose();
            _spawnedServer = null;
        }
    }

    private static async Task<bool> WaitForPortAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsPortListeningAsync(port, TimeSpan.FromMilliseconds(500))) return true;
            await Task.Delay(500);
        }
        return false;
    }

    private static async Task<bool> IsPortListeningAsync(int port, TimeSpan probeTimeout)
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = new CancellationTokenSource(probeTimeout);
            await tcp.ConnectAsync("127.0.0.1", port, cts.Token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}
