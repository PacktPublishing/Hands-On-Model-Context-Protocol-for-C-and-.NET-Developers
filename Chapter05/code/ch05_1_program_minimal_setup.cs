// Chapter 5 — Section 5.1.1
// Minimal ASP.NET Core MCP server: AddMcpServer, WithHttpTransport, WithTools<FlightTools>,
// OpenTelemetry pipeline, HttpClient for airline API, and MapMcp endpoint binding.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<FlightTools>();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();

builder.Services.AddHttpClient("AirlineApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Airline:ApiBaseUrl"] ?? "https://api.airline-partner.example.com");
});

builder.Services.AddScoped<IFlightSearchService, FlightSearchService>();
builder.Services.AddScoped<IFlightBookingService, FlightBookingService>();

builder.Services.AddHealthChecks()
    .AddCheck<AirlineApiHealthCheck>("airline-api", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapMcp();

app.Run();
