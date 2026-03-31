// Chapter 3 — Setup Your .NET MCP Workspace: SDKs, Inspector, and Dev Environment
//
// This project demonstrates two MCP server transport configurations:
//
//   --mode stdio (default) — stdio transport for MCP Inspector testing
//   --mode http            — HTTP transport for Docker Compose and Inspector HTTP mode
//   --example stdio        — Run original ch03_1 stdio example
//   --example http         — Run original ch03_2 HTTP example
//
// Examples:
//   dotnet run                      # Starts stdio server (default)
//   dotnet run -- --mode stdio      # Explicitly start stdio server
//   dotnet run -- --mode http       # Start HTTP server
//   dotnet run -- --example stdio   # Run ch03_1 example
//   dotnet run -- --example http    # Run ch03_2 example
//
// MCP Inspector connections:
//   stdio: npx @modelcontextprotocol/inspector dotnet run --project HandsOnMCPCSharp/Chapter03/code
//   HTTP:  Connect Inspector to http://localhost:5001/mcp
//
// See README.md for full setup instructions.

using TravelBooking.Chapter03;
using TravelBooking.Chapter03.Examples;
using TravelBooking.CodeSamples.Shared;

// Parse command line arguments
var mode = args.Contains("--mode") 
    ? args[Array.IndexOf(args, "--mode") + 1].ToLowerInvariant()
    : "stdio";

var example = args.Contains("--example")
    ? args[Array.IndexOf(args, "--example") + 1].ToLowerInvariant()
    : null;

Console.WriteLine($"╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine($"║     Chapter 3 — MCP Server {(example != null ? "Example" : "Main"),8}                   ║");
Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Run examples if requested
if (example == "stdio")
{
    await StdioServerExample.RunAsync(args);
    return;
}
else if (example == "http")
{
    await HttpServerExample.RunAsync(args);
    return;
}

// Otherwise run the main dual-mode server
if (mode == "http")
{
    await RunHttpServerAsync(args);
}
else
{
    await RunStdioServerAsync(args);
}

// ============================================================================
// Stdio Server Configuration (Section 3.1)
// ============================================================================
static async Task RunStdioServerAsync(string[] args)
{
    Console.WriteLine("Starting MCP server with stdio transport...");
    Console.WriteLine("Ready for MCP Inspector connection via stdio");
    Console.WriteLine();

    var builder = Host.CreateApplicationBuilder(args);

    // Register mock service
    builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();

    // Configure MCP server with stdio transport
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<FlightTools>();

    // Log to stderr so stdout remains clean for stdio protocol
    builder.Logging.AddConsole(options =>
        options.LogToStandardErrorThreshold = LogLevel.Trace);

    await builder.Build().RunAsync();
}

// ============================================================================
// HTTP Server Configuration (Section 3.3)
// ============================================================================
static async Task RunHttpServerAsync(string[] args)
{
    Console.WriteLine("Starting MCP server with HTTP transport...");
    Console.WriteLine("Server will be available at: http://localhost:5001/mcp");
    Console.WriteLine("Connect MCP Inspector to this endpoint");
    Console.WriteLine();

    var builder = WebApplication.CreateBuilder(args);

    // Register mock service
    builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();

    // Configure MCP server with HTTP transport
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithTools<FlightTools>();

    var app = builder.Build();

    // MapMcp registers the MCP endpoint at /mcp
    app.MapMcp("/mcp");

    // Show server is ready
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Console.WriteLine("✓ HTTP server ready!");
        Console.WriteLine("  Endpoint: http://localhost:5001/mcp");
        Console.WriteLine("  Press Ctrl+C to stop");
        Console.WriteLine();
    });

    await app.RunAsync();
}

