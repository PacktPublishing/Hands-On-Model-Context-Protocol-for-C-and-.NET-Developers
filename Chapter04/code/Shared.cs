// Shared domain models for Chapter 04 reference snippets.
// Full implementations are built in Chapter 5.

namespace TravelBooking.CodeSamples.Shared;

// ── Flight domain ─────────────────────────────────────────────────────────────

public record Money(decimal Amount, string CurrencyCode);

public record FlightOption(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    Money Price,
    int SeatsAvailable);

public record FlightSearchResult(List<FlightOption> Flights, int TotalResults);

public record PassengerInput(string GivenName, string FamilyName, string PassportNumber);

public record BookingConfirmation(
    string BookingReference,
    string FlightId,
    string Status,
    DateTimeOffset ConfirmedAt);

public record CancellationResult(
    string BookingReference,
    string Status,
    DateTimeOffset CancelledAt);

// ── Hotel domain ──────────────────────────────────────────────────────────────

public record HotelOption(
    string HotelId,
    string Name,
    string City,
    int StarRating,
    Money PricePerNight,
    int RoomsAvailable);

public record HotelSearchResult(List<HotelOption> Hotels, int TotalResults);

// ── Payment domain ────────────────────────────────────────────────────────────

public record PaymentResult(
    string PaymentId,
    string BookingReference,
    Money Amount,
    string Status,
    DateTimeOffset ProcessedAt);

// ── Itinerary domain ──────────────────────────────────────────────────────────

public record ItineraryItem(string Type, string ReferenceId, string Description);

public record Itinerary(
    string BookingReference,
    List<ItineraryItem> Items,
    string Status,
    DateTimeOffset CreatedAt);
