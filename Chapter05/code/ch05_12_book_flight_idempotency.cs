// Chapter 5 — Section 5.3.3
// Idempotency guard at the top of the BookFlight handler.
// The store is checked before any business logic executes.
// A cache hit returns the original BookingConfirmation without calling the airline again,
// preventing duplicate bookings when the client retries after a network timeout.

using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed partial class FlightTools
{
    [McpServerTool, Description(
        "Book a specific flight — idempotent via caller-supplied key. " +
        "Repeated calls with the same idempotency key return the original result.")]
    public async Task<BookingConfirmation> BookFlightIdempotent(
        [Description("Flight identifier returned by SearchFlights")] string flightId,
        [Description("Caller-supplied idempotency key (UUID recommended)")] string idempotencyKey,
        [Description("Passenger details list")] IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken = default)
    {
        // Return the cached result if this key was already processed
        var cached = await _idempotencyStore.GetAsync<BookingConfirmation>(
            idempotencyKey, cancellationToken);
        if (cached is not null)
            return cached;

        // Process the booking for the first time
        var confirmation = await _bookingService.BookAsync(
            flightId, passengers, cancellationToken);

        // Store the result for 24 hours; any retry within that window gets the same result
        await _idempotencyStore.SetAsync(
            idempotencyKey, confirmation, TimeSpan.FromHours(24), cancellationToken);

        return confirmation;
    }
}
