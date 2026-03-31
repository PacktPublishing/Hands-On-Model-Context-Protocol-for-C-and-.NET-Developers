// Shared types for Chapter 03
// Reuses types from Chapter 2 with mock service implementation

using Microsoft.Extensions.AI;

namespace TravelBooking.CodeSamples.Shared;

// ============================================================================
// Domain Models
// ============================================================================

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

// ============================================================================
// Service Interface
// ============================================================================

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

/// <summary>
/// Mock flight search service for Chapter 3 demonstrations
/// </summary>
public class MockFlightSearchService : IFlightSearchService
{
    public Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        DateOnly date,
        CancellationToken ct = default)
    {
        var flights = new List<FlightOption>
        {
            new FlightOption(
                FlightId: $"FL-{origin}-{destination}-001",
                Airline: "British Airways",
                FlightNumber: "BA123",
                DepartureTime: date.ToDateTime(new TimeOnly(8, 30)).ToUniversalTime(),
                ArrivalTime: date.ToDateTime(new TimeOnly(11, 45)).ToUniversalTime(),
                Price: new Money(299.99m, "GBP"),
                SeatsAvailable: 45
            ),
            new FlightOption(
                FlightId: $"FL-{origin}-{destination}-002",
                Airline: "Virgin Atlantic",
                FlightNumber: "VS456",
                DepartureTime: date.ToDateTime(new TimeOnly(14, 15)).ToUniversalTime(),
                ArrivalTime: date.ToDateTime(new TimeOnly(17, 30)).ToUniversalTime(),
                Price: new Money(349.99m, "GBP"),
                SeatsAvailable: 12
            ),
            new FlightOption(
                FlightId: $"FL-{origin}-{destination}-003",
                Airline: "Lufthansa",
                FlightNumber: "LH789",
                DepartureTime: date.ToDateTime(new TimeOnly(18, 45)).ToUniversalTime(),
                ArrivalTime: date.ToDateTime(new TimeOnly(22, 00)).ToUniversalTime(),
                Price: new Money(279.99m, "GBP"),
                SeatsAvailable: 23
            )
        };

        var result = new FlightSearchResult(flights, flights.Count);
        return Task.FromResult(result);
    }
}
