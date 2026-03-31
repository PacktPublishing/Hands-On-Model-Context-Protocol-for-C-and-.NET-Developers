// Chapter 2 - Section 2.3.1
// BookFlightTool demonstrating both successful booking confirmation and business-level error handling.
// The tool returns a string result that the LLM can interpret directly.
// Both the success and failure paths produce descriptive strings so the host can present them as-is.

using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

[McpServerToolType]
public sealed class BookFlightTool(IFlightBookingService bookingService)
{
    [McpServerTool, Description(
        "Book a seat on a specific flight for a single passenger. " +
        "Returns a confirmation reference on success or a descriptive failure message.")]
    public async Task<string> BookFlightAsync(
        [Description("The unique flight identifier returned by SearchFlights.")]
        string flightId,

        [Description("Passenger first name as it appears on the passport.")]
        string firstName,

        [Description("Passenger last name as it appears on the passport.")]
        string lastName,

        [Description("Passport number for identity verification.")]
        string passportNumber,

        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(flightId))
            return "Booking failed: flightId must not be empty.";

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return "Booking failed: both first name and last name are required.";

        var passenger = new PassengerInput(firstName, lastName, passportNumber, string.Empty);

        BookingConfirmation confirmation;
        try
        {
            confirmation = await bookingService.BookAsync(flightId, [passenger], ct);
        }
        catch (FlightNotAvailableException ex)
        {
            return $"Booking failed: flight '{ex.FlightId}' is no longer available. " +
                   "Please search again and choose an alternative.";
        }
        catch (OperationCanceledException)
        {
            return "Booking was cancelled before it could complete. Please try again.";
        }

        return $"Booking confirmed. Reference: {confirmation.BookingReference}. " +
               $"Flight: {confirmation.FlightId}. " +
               $"Passenger: {string.Join(", ", confirmation.PassengerNames)}. " +
               $"Total: {confirmation.TotalPrice}. " +
               $"Status: {confirmation.Status}.";
    }
}
