// Chapter 11 — Section 11.2.1
// Reusable MCP capability library structured around a single IMcpServerBuilder extension method.
// The library owns tool handlers, input types, and internal service registrations.
// The consuming server project registers the library with one chained call:
//   services.AddMcpServer().WithHttpTransport().AddFlightSearchCapabilities();
// No Program.cs, no Dockerfile, no transport configuration belongs in the library.

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace TravelBooking.Flights.Capabilities;

public static class FlightSearchCapabilitiesExtensions
{
    // Single integration point: registers tool handlers and internal dependencies.
    public static IMcpServerBuilder AddFlightSearchCapabilities(
        this IMcpServerBuilder builder)
    {
        // Internal services that tool handlers depend on
        builder.Services.AddSingleton<IAirportLookupService, AirportLookupService>();
        builder.Services.AddHttpClient<IAirlineApiClient, AirlineApiClient>();

        // Tool handler types registered through the MCP pipeline
        builder.WithTools<SearchFlightsTool>();
        builder.WithTools<GetFlightStatusTool>();

        return builder;
    }
}

// Tool handler — lives in the library, not the consuming server project.
// The [McpServerToolType] attribute marks the class for MCP tool discovery.
[McpServerToolType]
public sealed class SearchFlightsTool(
    IAirlineApiClient airline,
    IAirportLookupService airports)
{
    [McpServerTool(Name = "search_flights",
        Description = "Search available flights between two airports on a given date.")]
    public async Task<IEnumerable<FlightOption>> SearchAsync(
        [Description("IATA origin airport code, e.g. LHR")] string origin,
        [Description("IATA destination airport code, e.g. JFK")] string destination,
        [Description("Departure date in ISO 8601 format, e.g. 2026-09-15")] DateOnly departureDate,
        CancellationToken ct = default)
    {
        airports.Validate(origin, destination);
        return await airline.SearchAsync(origin, destination, departureDate, ct);
    }
}

[McpServerToolType]
public sealed class GetFlightStatusTool(IAirlineApiClient airline)
{
    [McpServerTool(Name = "get_flight_status",
        Description = "Retrieve the current status of a booked flight.")]
    public async Task<FlightStatus> GetStatusAsync(
        [Description("Airline booking reference")] string bookingReference,
        CancellationToken ct = default) =>
        await airline.GetStatusAsync(bookingReference, ct);
}

// Stub interfaces and types for compilation; replace with real implementations in the library.
public interface IAirportLookupService { void Validate(string origin, string destination); }
public interface IAirlineApiClient
{
    Task<IEnumerable<FlightOption>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct);
    Task<FlightStatus> GetStatusAsync(string bookingReference, CancellationToken ct);
}
public sealed class AirportLookupService : IAirportLookupService
{
    public void Validate(string origin, string destination) { }
}
public sealed class AirlineApiClient(HttpClient http) : IAirlineApiClient
{
    public Task<IEnumerable<FlightOption>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct) =>
        Task.FromResult(Enumerable.Empty<FlightOption>());
    public Task<FlightStatus> GetStatusAsync(string bookingReference, CancellationToken ct) =>
        Task.FromResult(new FlightStatus(bookingReference, "ON_TIME"));
}
public record FlightOption(string FlightNumber, string Origin, string Destination, DateOnly Date);
public record FlightStatus(string BookingReference, string Status);
