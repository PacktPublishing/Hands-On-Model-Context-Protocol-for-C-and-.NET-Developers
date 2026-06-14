// Chapter 7 — Section 7.1.1
// McpClient connection factory with environment-conditional transport selection.
// McpClient.CreateAsync performs the MCP initialize handshake internally.
// await using is required — DisposeAsync sends a graceful shutdown before closing the transport.
// No McpClientFactory or AddMcpClient extension method exists in the SDK.

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.Client;

public enum TransportMode { Stdio, Http }

public static class FlightsClientFactory
{
    public static Task<McpClient> CreateStdioClientAsync(
        string projectPath,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
        => McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = "dotnet",
                Arguments = ["run", "--project", projectPath, "--no-build"],
                Name = "FlightsServer",
                ShutdownTimeout = TimeSpan.FromSeconds(5)
            }),
            new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "TravelBookingClient", Version = "1.0" },
                InitializationTimeout = TimeSpan.FromSeconds(30)
            },
            loggerFactory,
            cancellationToken);

    public static Task<McpClient> CreateHttpClientAsync(
        Uri endpoint,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
        => McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                Name = "FlightsServer",
                MaxReconnectionAttempts = 5,
                DefaultReconnectionInterval = TimeSpan.FromSeconds(1)
            }),
            new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "TravelBookingClient", Version = "1.0" },
                InitializationTimeout = TimeSpan.FromSeconds(30)
            },
            loggerFactory,
            cancellationToken);

    // Resume an existing Streamable HTTP session after a transport reconnect.
    // Only valid when the server process is still running — skip if server restarted.
    public static Task<McpClient> ResumeHttpSessionAsync(
        Uri endpoint,
        McpClient previousClient,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
        => McpClient.ResumeSessionAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                Name = "FlightsServer"
            }),
            new ResumeClientSessionOptions
            {
                ServerCapabilities = previousClient.ServerCapabilities,
                ServerInfo = previousClient.ServerInfo
            },
            loggerFactory: loggerFactory,
            cancellationToken: cancellationToken);
}
