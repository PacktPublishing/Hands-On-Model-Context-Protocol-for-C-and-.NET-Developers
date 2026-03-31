// Chapter 5 — Section 5.3.2
// BookFlight with CancellationToken propagated through every downstream async call.
// Passing CancellationToken.None on any call breaks the cancellation chain and
// allows the downstream work to continue after the client disconnects.
// The SDK injects the token automatically when the parameter is named cancellationToken.

using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed partial class FlightTools
{
    [McpServerTool, Description(
        "Book a specific flight for one or more passengers. " +
        "Supply a caller-generated idempotency key to prevent duplicate bookings.")]
    public async Task<BookingConfirmation> BookFlight(
        [Description("Flight identifier returned by SearchFlights")] string flightId,
        [Description("Caller-supplied idempotency key (UUID recommended)")] string idempotencyKey,
        [Description("Passenger details list")] IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken = default)
    {
        // Step 1: confirm seats are still held — token propagated
        var availability = await _searchService.VerifyAvailabilityAsync(
            flightId, passengers.Count, cancellationToken);

        // Step 2: authorise payment — token propagated
        var payment = await _paymentClient.AuthorizeAsync(
            availability.Price, cancellationToken);

        // Step 3: confirm the booking with the airline — token propagated
        return await _bookingService.ConfirmAsync(
            flightId, payment.AuthCode, idempotencyKey, passengers, cancellationToken);
    }
}
