// Chapter 5 — FlightTools: the [McpServerToolType] class registered with the MCP server.
// Consolidates the patterns shown in ch05_5, ch05_10, ch05_11, ch05_12, ch05_13, and ch05_15.

using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TravelBooking.Chapter05;

[McpServerToolType]
public sealed class FlightTools
{
    private static readonly ActivitySource _activitySource = new("TravelBooking.FlightsServer");

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
    {
        using var activity = _activitySource.StartActivity("SearchFlights");
        activity?.SetTag("flight.origin", origin);
        activity?.SetTag("flight.destination", destination);
        activity?.SetTag("flight.departure_date", departureDate);
        activity?.SetTag("flight.passenger_count", passengerCount);

        var result = await _searchService.SearchAsync(
            origin, destination, departureDate, passengerCount, cancellationToken);

        activity?.SetTag("flight.options_returned", result.Options.Count);
        return result;
    }

    [McpServerTool, Description(
        "Stream available flight options as results arrive from each airline partner. " +
        "Results are emitted progressively rather than waiting for all airlines to respond.")]
    public async IAsyncEnumerable<FlightOption> SearchFlightsStreaming(
        [Description("IATA origin airport code (e.g. LHR)")] string origin,
        [Description("IATA destination airport code (e.g. JFK)")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _searchService.OpenResultChannel(
            origin, destination, departureDate, cancellationToken);

        await foreach (var option in channel.ReadAllAsync(cancellationToken))
            yield return option;
    }

    [McpServerTool, Description(
        "Book a specific flight — idempotent via caller-supplied key. " +
        "Repeated calls with the same idempotency key return the original result.")]
    public async Task<BookingConfirmation> BookFlight(
        [Description("Flight identifier returned by SearchFlights")] string flightId,
        [Description("Caller-supplied idempotency key (UUID recommended)")] string idempotencyKey,
        [Description("Passenger details list")] IReadOnlyList<PassengerInput> passengers,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new McpProtocolException("Idempotency key must not be empty.", McpErrorCode.InvalidParams);

        // Return cached result if this key was already processed
        var cached = await _idempotencyStore.GetAsync<BookingConfirmation>(idempotencyKey, cancellationToken);
        if (cached is not null)
            return cached;

        var confirmation = await _bookingService.BookAsync(flightId, passengers, cancellationToken);
        await _idempotencyStore.SetAsync(idempotencyKey, confirmation, TimeSpan.FromHours(24), cancellationToken);
        return confirmation;
    }

    [McpServerTool, Description("Cancel an existing flight booking and request a refund.")]
    public async Task<CancellationResult> CancelFlight(
        [Description("Booking reference returned by BookFlight")] string bookingReference,
        [Description("Reason for cancellation")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookingReference))
            throw new McpProtocolException("Booking reference must not be empty.", McpErrorCode.InvalidParams);

        var booking = await _bookingService.GetAsync(bookingReference, cancellationToken)
            ?? throw new McpProtocolException(
                $"Booking '{bookingReference}' was not found.",
                McpErrorCode.ResourceNotFound);

        if (booking.Status == "cancelled")
            throw new McpException($"Booking '{bookingReference}' is already cancelled.");

        return await _bookingService.CancelAsync(bookingReference, reason, cancellationToken);
    }
}
