// Chapter 5 — Travel Booking Server in ASP.NET Core.
//
// Working consolidation of the patterns shown across the seventeen ch05_*.cs reference
// snippets: minimal hosting, environment-conditional DI, typed options, health checks,
// attribute-based tool registration, streaming, idempotency, McpException error
// handling, and capability validation.
//
// Run:
//   dotnet run                      # binds to http://localhost:5002/mcp
//   curl http://localhost:5002/health/live
//   curl http://localhost:5002/health/ready
//
// Connect MCP Inspector to http://localhost:5002/mcp.
//
// The seventeen ch05_*.cs files in this folder are the verbatim chapter listings and
// are excluded from compilation by Chapter05.csproj. See README.md for descriptions.

using Microsoft.Extensions.Options;
using TravelBooking.Chapter05;

var builder = WebApplication.CreateBuilder(args);

// Bind to a fixed port so MCP Inspector / curl have a predictable URL
builder.WebHost.UseUrls("http://0.0.0.0:5002");

// ── Typed options (Section 5.1.3) ───────────────────────────────────────────
builder.Services.Configure<AirlineOptions>(builder.Configuration.GetSection("Airline"));

// ── HttpClient for the airline partner API (Section 5.1.1) ──────────────────
builder.Services.AddHttpClient("AirlineApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AirlineOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl);
});

// ── Domain services (Section 5.1.2) ─────────────────────────────────────────
// Singleton in this dev sample — production would use Scoped + Redis-backed store
builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();
builder.Services.AddSingleton<IFlightBookingService, MockFlightBookingService>();

// IIdempotencyStore — environment-conditional registration
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
else
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>(); // RedisIdempotencyStore in prod

// ── Capability validation runs at startup (Section 5.2.3) ───────────────────
builder.Services.AddHostedService<CapabilityValidationService>();

// ── Health checks (Section 5.1.4) ───────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<AirlineApiHealthCheck>("airline-api", tags: ["ready"]);

// ── MCP server with HTTP transport (Section 5.1.1) ──────────────────────────
builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<FlightTools>();

// CORS so the browser-based MCP Inspector can connect
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

// Liveness: process is up
app.MapHealthChecks("/health/live");

// Readiness: tagged dependency checks must pass
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready"),
});

// MCP endpoint
app.MapMcp("/mcp");

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine("✓ Chapter 5 Travel Booking server ready");
    Console.WriteLine("  MCP endpoint:  http://localhost:5002/mcp");
    Console.WriteLine("  Liveness:      http://localhost:5002/health/live");
    Console.WriteLine("  Readiness:     http://localhost:5002/health/ready");
    Console.WriteLine("  Press Ctrl+C to stop.");
    Console.WriteLine();
});

app.Run();
