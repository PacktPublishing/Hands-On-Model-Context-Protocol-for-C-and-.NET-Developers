// Chapter 3 — Section 3.1
// Minimal FlightsMcpServer configured with stdio transport for local verification.
// This demonstrates the stdio transport pattern for early development.
// 
// To run this specific example, modify Program.cs to call StdioServerExample.RunAsync()

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Chapter03.Examples;

/// <summary>
/// Example of stdio transport configuration for MCP server.
/// This is a self-contained example showing the minimal setup needed.
/// </summary>
public static class StdioServerExample
{
    /// <summary>
    /// Runs a minimal MCP server with stdio transport.
    /// Used for local development and MCP Inspector connections.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        Console.WriteLine("Running Chapter 3 - Section 3.1: Stdio Transport Example");
        Console.WriteLine("========================================================");
        Console.WriteLine();

        var builder = Host.CreateApplicationBuilder(args);

        // Register the mock flight search service
        builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();

        // Configure MCP server with stdio transport and tools
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<FlightToolsStdio>();

        // Log to stderr so that stdout remains clean for the stdio protocol stream
        builder.Logging.AddConsole(options =>
            options.LogToStandardErrorThreshold = LogLevel.Trace);

        await builder.Build().RunAsync();
    }
}

/// <summary>
/// Tool class for stdio transport example.
/// Demonstrates how to define MCP tools that work with stdio transport.
/// </summary>
[McpServerToolType]
public class FlightToolsStdio(IFlightSearchService flights)
{
    [McpServerTool]
    [Description("Search available flights between two airports on a given date.")]
    public async Task<FlightSearchResult> SearchFlightsAsync(
        [Description("IATA origin airport code, e.g. LHR for London Heathrow.")]
        string origin,

        [Description("IATA destination airport code, e.g. JFK for New York.")]
        string destination,

        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")]
        string date,

        CancellationToken ct = default)
    {
        var departureDate = DateOnly.Parse(date);
        return await flights.SearchAsync(origin, destination, departureDate, ct);
    }
}
