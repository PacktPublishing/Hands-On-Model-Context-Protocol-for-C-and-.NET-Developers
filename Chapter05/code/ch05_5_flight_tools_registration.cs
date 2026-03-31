// Chapter 5 — Section 5.2.1
// FlightTools: attribute-based registration for all three flight capabilities.
// [McpServerToolType] marks the class for SDK scanning.
// [McpServerTool] + [Description] on each method registers it as an MCP tool
// and populates the JSON Schema description shown to LLM clients.

using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed class FlightTools
{
    private readonly IFlightSearchService _searchService;
    private readonly IFlightBookingService _bookingService;
    private readonly IIdempotencyStore _idempotencyStore;

    public FlightTools(
        IFlightSearchService searchService,
        IFlightBookingService bookingService,
        IIdempotencyStore idempotencyStore)
    {
        _searchService = searchService;
        _bookingService = bookingService;
        _idempotencyStore = idempotencyStore;
    }

    [McpServerTool, Description("Search for available flights between two airports on a given date.")]
    public async Task<FlightSearchResult> SearchFlights(
        [Description("IATA origin airport code (e.g. LHR)")] string origin,
        [Description("IATA destination airport code (e.g. JFK)")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [Description("Number of passengers (1-9)")] int passengerCount = 1,
        CancellationToken cancellationToken = default)
        => await _searchService.SearchAsync(
            origin, destination, departureDate, passengerCount, cancellationToken);

    [McpServerTool, Description("Book a specific flight for one or more passengers. Supply an idempotency key to prevent duplicate bookings.")]
    public async Task<BookingConfirmation> BookFlight(
        [Description("Flight identifier returned by SearchFlights")] string flightId,
        [Description("Caller-supplied idempotency key (UUID)")] string idempotencyKey,
        [Description("Passenger details list")] IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken = default)
        => await _bookingService.BookAsync(
            flightId, idempotencyKey, passengers, cancellationToken);

    [McpServerTool, Description("Cancel an existing flight booking and request a refund.")]
    public async Task<CancellationResult> CancelFlight(
        [Description("Booking reference returned by BookFlight")] string bookingReference,
        [Description("Reason for cancellation")] string reason,
        CancellationToken cancellationToken = default)
        => await _bookingService.CancelAsync(
            bookingReference, reason, cancellationToken);
}
