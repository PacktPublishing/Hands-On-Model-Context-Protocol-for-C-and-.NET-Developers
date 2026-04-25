// Chapter 3 — Section 3.3
// FlightsMcpServer reconfigured for HTTP transport.
// Required for Docker Compose orchestration where containers cannot share stdin/stdout streams.
//
// To run this specific example, modify Program.cs to call HttpServerExample.RunAsync()

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Chapter03.Examples;

/// <summary>
/// Example of HTTP transport configuration for MCP server.
/// This demonstrates how to expose MCP over HTTP for web and container scenarios.
/// </summary>
public static class HttpServerExample
{
    /// <summary>
    /// Runs an MCP server with HTTP transport on port 5001.
    /// Suitable for Docker Compose, web clients, and MCP Inspector HTTP mode.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        Console.WriteLine("Running Chapter 3 - Section 3.3: HTTP Transport Example");
        Console.WriteLine("========================================================");
        Console.WriteLine();

        var builder = WebApplication.CreateBuilder(args);

        // Force port 5001 - bind to all interfaces for MCP Inspector connectivity
        builder.WebHost.UseUrls("http://0.0.0.0:5001");

        // Register the mock flight search service
        builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();

        // Add CORS so browser-based MCP Inspector can connect
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Configure MCP server with HTTP transport
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<FlightToolsHttp>();

        var app = builder.Build();

        // Enable CORS - must be before MapMcp
        app.UseCors();

        // MapMcp registers the MCP endpoint at the given path prefix
        // The Inspector connects to http://localhost:5001/mcp in HTTP mode
        app.MapMcp("/mcp");

        // Show connection information
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("✓ HTTP MCP Server Started!");
            Console.WriteLine("  Endpoint: http://localhost:5001/mcp");
            Console.WriteLine("  Connect MCP Inspector to this URL");
            Console.WriteLine("  Press Ctrl+C to stop");
            Console.WriteLine();
        });

        await app.RunAsync();
    }
}

/// <summary>
/// Tool class for HTTP transport example.
/// The tool implementation is identical to the stdio version — transport is independent
/// of capability registration. Only the server configuration differs.
/// </summary>
[McpServerToolType]
public class FlightToolsHttp(IFlightSearchService flights)
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
