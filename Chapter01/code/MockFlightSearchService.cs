// Mock implementation of IFlightSearchService for demonstration purposes
// In a real application, this would connect to an actual airline API

using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

/// <summary>
/// Mock flight search service that returns sample data
/// This demonstrates the MCP-compliant implementation
/// </summary>
public class MockFlightSearchService : IFlightSearchService
{
    public Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        DateOnly date,
        CancellationToken ct = default)
    {
        // Generate mock flight data
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
