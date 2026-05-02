// Chapter 5 — Travel Booking Server: shared domain models, service interfaces, mock implementations.
// Consolidates the patterns shown in ch05_2, ch05_3, ch05_6, and ch05_7 reference snippets.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;

namespace TravelBooking.Chapter05;

// ── Domain records (Section 5.2.2 / 5.2.3) ───────────────────────────────────

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

public record PassengerInput(
    [property: Description("Passenger first name as on passport")]
    [property: StringLength(50, MinimumLength = 1)]
    string FirstName,

    [property: Description("Passenger last name as on passport")]
    [property: StringLength(80, MinimumLength = 1)]
    string LastName,

    [property: Description("Machine-readable passport number (6-9 alphanumeric characters)")]
    [property: StringLength(9, MinimumLength = 6)]
    string PassportNumber,

    [property: Description("Date of birth in ISO 8601 format (YYYY-MM-DD)")]
    string DateOfBirth);

public record BookingConfirmation(
    string BookingReference,
    string FlightId,
    IReadOnlyList<string> PassengerNames,
    Money TotalPrice,
    DateTimeOffset BookedAt,
    string Status);

public record CancellationResult(
    string BookingReference,
    string Status,
    Money? RefundAmount,
    string Message);

// ── Typed options (Section 5.1.3) ────────────────────────────────────────────

public record AirlineOptions
{
    public string ApiBaseUrl { get; init; } = "https://api.airline-partner.example.com";
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
    public int MaxRetries { get; init; } = 2;
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

    ChannelReader<FlightOption> OpenResultChannel(
        string origin,
        string destination,
        string departureDate,
        CancellationToken cancellationToken);
}

public interface IFlightBookingService
{
    Task<BookingConfirmation> BookAsync(
        string flightId,
        IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken);

    Task<BookingConfirmation?> GetAsync(string bookingReference, CancellationToken cancellationToken);

    Task<CancellationResult> CancelAsync(string bookingReference, string reason, CancellationToken cancellationToken);
}

public interface IIdempotencyStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class;
}

// ── Mock implementations (development only) ──────────────────────────────────

public sealed class MockFlightSearchService : IFlightSearchService
{
    public Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        string departureDate,
        int passengerCount,
        CancellationToken cancellationToken)
    {
        var date = DateOnly.Parse(departureDate);
        var options = new List<FlightOption>
        {
            new($"FL-{origin}-{destination}-001", "British Airways", "BA123",
                date.ToDateTime(new TimeOnly(8, 30)).ToUniversalTime(),
                date.ToDateTime(new TimeOnly(11, 45)).ToUniversalTime(),
                new Money(299.99m * passengerCount, "GBP"), 45),
            new($"FL-{origin}-{destination}-002", "Virgin Atlantic", "VS456",
                date.ToDateTime(new TimeOnly(14, 15)).ToUniversalTime(),
                date.ToDateTime(new TimeOnly(17, 30)).ToUniversalTime(),
                new Money(349.99m * passengerCount, "GBP"), 12),
            new($"FL-{origin}-{destination}-003", "Lufthansa", "LH789",
                date.ToDateTime(new TimeOnly(18, 45)).ToUniversalTime(),
                date.ToDateTime(new TimeOnly(22, 0)).ToUniversalTime(),
                new Money(279.99m * passengerCount, "GBP"), 23),
        };
        var searchId = Guid.NewGuid().ToString("N");
        return Task.FromResult(new FlightSearchResult(options, searchId, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public ChannelReader<FlightOption> OpenResultChannel(
        string origin,
        string destination,
        string departureDate,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<FlightOption>();
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await SearchAsync(origin, destination, departureDate, 1, cancellationToken);
                foreach (var option in result.Options)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(150, cancellationToken);
                    await channel.Writer.WriteAsync(option, cancellationToken);
                }
            }
            catch (OperationCanceledException) { /* client disconnected */ }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);
        return channel.Reader;
    }
}

public sealed class MockFlightBookingService : IFlightBookingService
{
    private readonly Dictionary<string, BookingConfirmation> _bookings = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public Task<BookingConfirmation> BookAsync(
        string flightId,
        IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken)
    {
        var reference = $"BR-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var confirmation = new BookingConfirmation(
            reference,
            flightId,
            passengers.Select(p => $"{p.FirstName} {p.LastName}").ToList(),
            new Money(299.99m * passengers.Count, "GBP"),
            DateTimeOffset.UtcNow,
            "confirmed");
        lock (_gate) _bookings[reference] = confirmation;
        return Task.FromResult(confirmation);
    }

    public Task<BookingConfirmation?> GetAsync(string bookingReference, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_bookings.GetValueOrDefault(bookingReference));
    }

    public Task<CancellationResult> CancelAsync(string bookingReference, string reason, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_bookings.TryGetValue(bookingReference, out var booking))
                return Task.FromResult(new CancellationResult(bookingReference, "not_found", null, "Booking not found."));

            var cancelled = booking with { Status = "cancelled" };
            _bookings[bookingReference] = cancelled;
            return Task.FromResult(new CancellationResult(
                bookingReference, "cancelled", cancelled.TotalPrice, $"Cancelled: {reason}"));
        }
    }
}

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, (object Value, DateTimeOffset Expires)> _store = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        lock (_gate)
        {
            if (_store.TryGetValue(key, out var entry) && entry.Expires > DateTimeOffset.UtcNow)
                return Task.FromResult((T?)entry.Value);

            _store.Remove(key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class
    {
        lock (_gate) _store[key] = (value, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }
}
