// Chapter 11 — Section 11.4
// FaultInjectionServer: configures stub tools to return controlled errors
// so agent fault-handling paths can be exercised in isolation.

using System.IO.Pipelines;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

namespace TravelBooking.Testing;

/// <summary>Describes the error a faulted tool should return.</summary>
public sealed record FaultSpec(
    string ErrorMessage,
    bool IsPermanent = true);

/// <summary>
/// An in-process MCP server where every registered tool returns an error result.
/// Pass to the agent under test to verify graceful degradation.
/// </summary>
public sealed class FaultInjectionServer : IAsyncDisposable
{
    private readonly McpServer _server;
    private readonly Task _serverTask;

    private FaultInjectionServer(
        McpServer server, Task serverTask, McpClient client)
    {
        _server     = server;
        _serverTask = serverTask;
        Client      = client;
    }

    public McpClient Client { get; }

    public static async Task<FaultInjectionServer> CreateAsync(
        Dictionary<string, FaultSpec> faults,
        CancellationToken ct = default)
    {
        Pipe clientToServer = new(), serverToClient = new();

        // Build a stub tool per faulted name that always returns IsError = true.
        var tools = faults.Select(kvp =>
        {
            var message = kvp.Value.ErrorMessage;
            return McpServerTool.Create(
                () => Task.FromResult(
                    new CallToolResponse(
                        IsError: true,
                        Content: [new TextContent(message)])),
                new McpServerToolCreateOptions { Name = kvp.Key });
        }).ToArray();

        var server = McpServer.Create(
            new StreamServerTransport(
                clientToServer.Reader.AsStream(),
                serverToClient.Writer.AsStream()),
            new McpServerOptions { ToolCollection = [.. tools] });

        var serverTask = server.RunAsync(ct);

        var client = await McpClient.CreateAsync(
            new StreamClientTransport(
                clientToServer.Writer.AsStream(),
                serverToClient.Reader.AsStream()),
            cancellationToken: ct);

        return new(server, serverTask, client);
    }

    /// <summary>
    /// Creates a partial fault server: specified tools return errors,
    /// remaining tools are wired from a healthy stub list.
    /// </summary>
    public static async Task<FaultInjectionServer> CreatePartialAsync(
        Dictionary<string, FaultSpec> faults,
        IReadOnlyList<McpServerTool> healthyTools,
        CancellationToken ct = default)
    {
        Pipe clientToServer = new(), serverToClient = new();

        var faultedTools = faults.Select(kvp =>
        {
            var message = kvp.Value.ErrorMessage;
            return McpServerTool.Create(
                () => Task.FromResult(
                    new CallToolResponse(true,
                        [new TextContent(message)])),
                new McpServerToolCreateOptions { Name = kvp.Key });
        });

        var combined = faultedTools.Concat(healthyTools).ToArray();

        var server = McpServer.Create(
            new StreamServerTransport(
                clientToServer.Reader.AsStream(),
                serverToClient.Writer.AsStream()),
            new McpServerOptions { ToolCollection = [.. combined] });

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
