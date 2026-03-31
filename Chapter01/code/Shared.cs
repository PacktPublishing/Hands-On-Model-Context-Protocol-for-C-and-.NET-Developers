// Shared types for Chapter 01 examples
// These types represent the domain models used across both pre-MCP and MCP examples

namespace TravelBooking.CodeSamples.Shared;

/// <summary>
/// Represents a flight option returned from search
/// </summary>
public record FlightOption(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    Money Price,
    int SeatsAvailable);

/// <summary>
/// Represents a monetary value with currency
/// </summary>
public record Money(decimal Amount, string CurrencyCode);

/// <summary>
/// Result of a flight search operation
/// </summary>
public record FlightSearchResult(List<FlightOption> Flights, int TotalResults);

/// <summary>
/// Service interface for flight search operations
/// </summary>
public interface IFlightSearchService
{
    Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        DateOnly date,
        CancellationToken ct = default);
}
