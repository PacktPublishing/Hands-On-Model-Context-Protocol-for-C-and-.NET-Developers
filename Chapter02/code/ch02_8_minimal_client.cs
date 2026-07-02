// Chapter 2 — Section 2.1 Roles and message flows
// Minimal MCP client: connection, capability discovery, and tool invocation.
// McpClient.CreateAsync completes the initialization handshake before returning,
// so the session is fully ready and all server capabilities are advertised by the
// time the first ListToolsAsync or CallToolAsync call is made.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace TravelBooking.Chapter02;

// ── Connect and discover ────────────────────────────────────────────────────
var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Travel Booking Server",
    Command = "dotnet",
    Arguments = ["run", "--project", "TravelBooking.FlightsServer"]
});

await using var client = await McpClient.CreateAsync(transport);

var tools = await client.ListToolsAsync();
foreach (var tool in tools)
    Console.WriteLine($"{tool.Name}: {tool.Description}");

// ── Invoke a tool ───────────────────────────────────────────────────────────
var result = await client.CallToolAsync(
    "SearchFlights",
    new Dictionary<string, object?>
    {
        ["origin"]      = "LHR",
        ["destination"] = "JFK",
        ["date"]        = "2026-06-15"
    });

foreach (var content in result.Content)
    Console.WriteLine(content.Text);
