// Chapter 1 — Section 1.4
// Manual HTTP integration before MCP: tight coupling, bespoke auth, brittle contracts.
// Every new consumer must know the URL structure, the auth header, and the response mapping.
// Compare with ch01_2_with_mcp_search_flights.cs to see what MCP removes.

using System.Net.Http.Json;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

/// <summary>
/// Pre-MCP flight search: the consumer owns the entire integration surface.
/// Authentication, URL construction, and response mapping are all caller responsibilities.
/// Adding a new consumer (Blazor UI, background worker, LLM host) means duplicating this.
/// </summary>
public class FlightSearchService(HttpClient http)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task<List<FlightOption>> SearchAsync(
        string origin,
        string destination,
        DateOnly date,
        CancellationToken ct = default)
    {
        // Consumer must know which header name and env var to use
        http.DefaultRequestHeaders.TryAddWithoutValidation(
            ApiKeyHeader, Environment.GetEnvironmentVariable("AIRLINE_API_KEY") ?? string.Empty);

        // Consumer must know the URL convention — changes break all consumers silently
        var url = $"/flights?from={origin}&to={destination}&date={date:yyyy-MM-dd}";

        var response = await http.GetFromJsonAsync<AirlineApiResponse>(url, ct)
            ?? throw new InvalidOperationException("Airline API returned no response.");

        return response.Flights
            .Select(f => new FlightOption(
                f.FlightId,
                f.Airline,
                f.FlightNumber,
                f.DepartureTime,
                f.ArrivalTime,
                new Money(f.PriceGbp, "GBP"),
                f.SeatsAvailable))
            .ToList();
    }
}

// Internal response shape — must be maintained per-consumer, per-service
file record AirlineApiResponse(List<AirlineFlight> Flights);
file record AirlineFlight(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    decimal PriceGbp,
    int SeatsAvailable);
