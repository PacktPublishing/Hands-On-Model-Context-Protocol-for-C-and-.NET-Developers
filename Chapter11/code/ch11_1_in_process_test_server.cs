// Chapter 11 — Section 11.1
// InProcessTestServer: creates a matched McpServer + McpClient pair using two
// System.IO.Pipelines.Pipe instances as the transport. No network, no process boundary.
// Dispose completes both pipes and the server stops cleanly.

using System.IO.Pipelines;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

namespace TravelBooking.Testing;

/// <summary>
/// A disposable in-process MCP server+client pair for use in unit tests.
/// The server runs on a background task; the client is ready to use immediately.
/// </summary>
public sealed class InProcessTestServer : IAsyncDisposable
{
    private readonly McpServer _server;
    private readonly Task _serverTask;

    private InProcessTestServer(McpServer server, Task serverTask, McpClient client)
    {
        _server    = server;
        _serverTask = serverTask;
        Client     = client;
    }

    public McpClient Client { get; }

    /// <summary>
    /// Creates the in-process server with the supplied stub tool collection.
    /// Call server.RunAsync() is fired-and-forgotten intentionally: the server
    /// processes requests in the background and stops when the pipe is completed.
    /// </summary>
    public static async Task<InProcessTestServer> CreateAsync(
        IReadOnlyList<McpServerTool> stubTools,
        CancellationToken ct = default)
    {
        Pipe clientToServer = new();
        Pipe serverToClient = new();

        var server = McpServer.Create(
            new StreamServerTransport(
                clientToServer.Reader.AsStream(),
                serverToClient.Writer.AsStream()),
            new McpServerOptions
            {
                ToolCollection = [.. stubTools]
            });

        var serverTask = server.RunAsync(ct);

        var client = await McpClient.CreateAsync(
            new StreamClientTransport(
                clientToServer.Writer.AsStream(),
                serverToClient.Reader.AsStream()),
            cancellationToken: ct);

        return new(server, serverTask, client);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await _server.DisposeAsync();
        try { await _serverTask; }
        catch (OperationCanceledException) { }
    }
}

/// <summary>Pre-built stub tools for common Travel Booking scenarios.</summary>
public static class TravelStubs
{
    public static McpServerTool SearchFlights() =>
        McpServerTool.Create(
            (string origin, string destination, string date) =>
                $"""[{{"id":"SU123","origin":"{origin}","destination":"{destination}",
                      "date":"{date}","price":450.00,"cabin":"economy"}}]""",
            new McpServerToolCreateOptions { Name = "search_flights" });

    public static McpServerTool BookFlight() =>
        McpServerTool.Create(
            (string flightId, string passengerName) =>
                $"""{{ "reference":"REF-001","flight_id":"{flightId}",
                       "passenger":"{passengerName}","status":"confirmed" }}""",
            new McpServerToolCreateOptions { Name = "book_flight" });

    public static McpServerTool GetItinerary() =>
        McpServerTool.Create(
            (string sessionId) =>
                $"""{{ "session_id":"{sessionId}", "legs":[] }}""",
            new McpServerToolCreateOptions { Name = "get_itinerary" });
}
