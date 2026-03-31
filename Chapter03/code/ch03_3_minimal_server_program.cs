// Chapter 3 — Section 3.2.3
// Minimal stdio MCP server: logging redirected to stderr, tools and resources registered.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Redirect all log output to stderr; stdout belongs to the MCP protocol stream.
builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FlightTools>()
    .WithResourcesFromAssembly();

await builder.Build().RunAsync();
