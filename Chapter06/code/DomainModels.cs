// Chapter 6 — Domain models and service interfaces consumed by the validated and
// sanitised tool handlers. Mirrors the shape used in Chapter 5 so that the tests
// in ../tests/ exercise patterns that integrate cleanly with the FlightsServer.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TravelBooking.Chapter06;

// ── Tool input records (Section 6.4.2) ───────────────────────────────────────

public record SearchFlightsInput(
    [property: Description("IATA origin airport code (e.g. LHR)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Origin,

    [property: Description("IATA destination airport code (e.g. AMS)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Destination,

    [property: Description("Departure date in ISO 8601 format (YYYY-MM-DD)")]
    string DepartureDate,

    [property: Description("Number of passengers (1-9)")]
    [property: Range(1, 9)]
    int PassengerCount = 1);

public record BookFlightInput(
    [property: Description("Flight identifier returned by search_flights")]
    string FlightId,

    [property: Description("Caller-supplied idempotency key (UUID v4)")]
    string IdempotencyKey,

    [property: Description("Passenger full name")]
    [property: StringLength(100, MinimumLength = 2)]
    string PassengerName,

    [property: Description("Passenger passport number")]
    string PassportNumber);

public record CancelFlightInput(
    [property: Description("Booking reference returned by book_flight")]
    string BookingReference,

    [property: Description("Reason for cancellation")]
    [property: StringLength(500)]
    string Reason);

// ── Tool output records ──────────────────────────────────────────────────────

public record Money(decimal Amount, string Currency);

public record FlightOption(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    Money Price,
    int SeatsAvailable);

public record FlightSearchResult(
    IReadOnlyList<FlightOption> Options,
    string SearchId,
    DateTimeOffset ExpiresAt);

public record BookingConfirmation(
    string BookingReference,
    string FlightId,
    string PassengerName,
    Money TotalPrice,
    DateTimeOffset BookedAt,
    string Status);

public record CancellationResult(
    string BookingReference,
    string Status,
    Money? RefundAmount,
    string Message);

// ── Domain exception used by the sanitisation filter (Section 6.4.4) ─────────

public sealed class FlightNotAvailableException : Exception
{
    public FlightNotAvailableException(string message) : base(message) { }
    public FlightNotAvailableException(string message, Exception inner) : base(message, inner) { }
}

// ── Service interfaces ───────────────────────────────────────────────────────

public interface IFlightSearchService
{
    Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        string departureDate,
        int passengerCount,
        CancellationToken cancellationToken);
}

public interface IFlightBookingService
{
    Task<BookingConfirmation> BookAsync(
        string flightId,
        string idempotencyKey,
        string passengerName,
        string passportNumber,
        CancellationToken cancellationToken);

    Task<CancellationResult> CancelAsync(
        string bookingReference,
        string reason,
        CancellationToken cancellationToken);
}
