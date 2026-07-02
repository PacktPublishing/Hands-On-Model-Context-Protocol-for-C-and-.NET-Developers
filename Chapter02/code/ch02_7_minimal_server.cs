// Chapter 2 — Section 2.1 Roles and message flows
// Minimal stdio MCP server: host setup and FlightTools capability registration.
// LogToStandardErrorThreshold redirects all logs to stderr, keeping stdout clean
// for the JSON-RPC message stream — mandatory for every stdio server.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TravelBooking.Chapter02;

// ── Host setup ─────────────────────────────────────────────────────────────
// Program.cs entry point
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FlightTools>();

await builder.Build().RunAsync();

// ── Tool class ─────────────────────────────────────────────────────────────
[McpServerToolType]
public class FlightTools
{
    [McpServerTool]
    [Description("Search for available flights between two airports on a given date.")]
    public static async Task<string> SearchFlights(
        HttpClient http,
        [Description("IATA origin airport code, e.g. LHR.")] string origin,
        [Description("IATA destination code, e.g. JFK.")] string destination,
        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")] string date)
    {
        var url = $"/flights?from={origin}&to={destination}&date={date}";
        return await http.GetStringAsync(url);
    }
}
