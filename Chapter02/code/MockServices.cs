// Mock service implementations for Chapter 02 demonstration

using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

/// <summary>
/// Mock flight search service for demonstration
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
            )
        };

        var result = new FlightSearchResult(flights, flights.Count);
        return Task.FromResult(result);
    }
}

/// <summary>
/// Mock booking service for demonstration
/// </summary>
public class MockFlightBookingService : IFlightBookingService
{
    private readonly HashSet<string> _bookedFlights = new();

    public Task<BookingConfirmation> BookAsync(
        string flightId,
        List<PassengerInput> passengers,
        CancellationToken ct = default)
    {
        // Simulate flight availability check
        if (_bookedFlights.Contains(flightId))
        {
            throw new FlightNotAvailableException(flightId, $"Flight {flightId} is fully booked");
        }

        // Generate booking reference
        var bookingRef = $"BK-{Guid.NewGuid():N}"[..16].ToUpper();
        
        // Mark flight as booked (in real system, would reduce seat count)
        _bookedFlights.Add(flightId);

        var confirmation = new BookingConfirmation(
            BookingReference: bookingRef,
            FlightId: flightId,
            PassengerNames: passengers.Select(p => $"{p.FirstName} {p.LastName}").ToList(),
            TotalPrice: "299.99 GBP",
            Status: "Confirmed"
        );

        return Task.FromResult(confirmation);
    }
}

/// <summary>
/// Mock itinerary service for demonstration
/// </summary>
public class MockItineraryService : IItineraryService
{
    private readonly Dictionary<string, ItineraryDetails> _itineraries = new();

    public MockItineraryService()
    {
        // Pre-populate with sample itinerary
        _itineraries["BK-SAMPLE123"] = new ItineraryDetails(
            BookingReference: "BK-SAMPLE123",
            FlightId: "FL-LHR-JFK-001",
            Origin: "LHR",
            Destination: "JFK",
            DepartureTime: DateTime.UtcNow.AddDays(30).AddHours(8),
            ArrivalTime: DateTime.UtcNow.AddDays(30).AddHours(16),
            PassengerNames: new List<string> { "John Doe", "Jane Doe" },
            TotalPrice: new Money(599.98m, "GBP"),
            Status: "Confirmed"
        );
    }

    public Task<ItineraryDetails?> GetAsync(
        string bookingReference,
        CancellationToken ct = default)
    {
        _itineraries.TryGetValue(bookingReference, out var itinerary);
        return Task.FromResult(itinerary);
    }

    public void AddItinerary(ItineraryDetails itinerary)
    {
        _itineraries[itinerary.BookingReference] = itinerary;
    }
}
