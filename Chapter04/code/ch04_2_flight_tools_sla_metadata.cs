// Chapter 4 — Section 4.4.3
// SLA metadata embedded in MCP capability descriptors.
// The Description field is read by LLM agents during capability discovery.
// Embedding p95 latency, availability, and retry guidance makes performance
// characteristics visible to automated consumers without out-of-band documentation.

using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TravelBooking.CodeSamples.Shared;

[McpServerToolType]
public sealed class FlightTools
{
    [McpServerTool(Name = "SearchFlightsTool", Description =
        "Search available flights by route and departure date. " +
        "SLA: p95 300ms, availability 99.9%. Retryable up to 2 times " +
        "with exponential backoff and jitter.")]
    public async Task<FlightSearchResult> SearchFlightsAsync(
        [Description("Departure airport IATA code, e.g. LHR")] string origin,
        [Description("Arrival airport IATA code, e.g. JFK")] string destination,
        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15")] string date,
        CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in Chapter 5.");

    [McpServerTool(Name = "BookFlightTool", Description =
        "Book a specific flight offer returned by SearchFlightsTool. " +
        "SLA: p95 500ms, availability 99.9%. " +
        "Idempotent: supply a client-generated bookingReference on every attempt. " +
        "The server deduplicates on this key so retries are safe.")]
    public async Task<BookingConfirmation> BookFlightAsync(
        [Description("Flight offer ID from SearchFlightsTool result")] string flightOfferId,
        [Description("Client-generated UUID used as idempotency key")] string bookingReference,
        [Description("Lead passenger details")] PassengerInput passenger,
        CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in Chapter 5.");

    [McpServerTool(Name = "CancelFlightTool", Description =
        "Cancel an existing flight booking by booking reference. " +
        "SLA: p95 250ms, availability 99.9%. Retryable up to 2 times. " +
        "Cancellation is idempotent: cancelling an already-cancelled booking " +
        "returns the original cancellation result without error.")]
    public async Task<CancellationResult> CancelFlightAsync(
        [Description("Booking reference returned by BookFlightTool")] string bookingReference,
        CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in Chapter 5.");
}
