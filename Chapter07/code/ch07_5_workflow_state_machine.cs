// Chapter 7 — Section 7.3.1 / 7.3.2
// Explicit typed workflow state machine for the travel booking domain.
// BookingWorkflowContext is immutable — each transition returns a new `with` copy.
// The CorrelationId is set once at creation and preserved across all transitions,
// so all log entries and cache keys for a booking share the same identifier.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Client.Workflow;

public enum WorkflowState
{
    FlightSearch,
    FlightSelected,
    FlightBooked,
    Confirmed,
    Failed
}

public sealed record BookingWorkflowContext
{
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
    public WorkflowState State { get; init; } = WorkflowState.FlightSearch;
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public string? DepartureDate { get; init; }
    public string? PassengerName { get; init; }
    public string? SelectedFlightId { get; init; }
    public string? BookingReference { get; init; }
    public DateTimeOffset? BookedAt { get; init; }
    public string? ItineraryJson { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class FlightBookingWorkflow
{
    private readonly McpClient _client;

    public FlightBookingWorkflow(McpClient client) => _client = client;

    public async Task<BookingWorkflowContext> RunAsync(
        BookingWorkflowContext ctx, CancellationToken cancellationToken = default)
    {
        ctx = await SearchFlightsStepAsync(ctx, cancellationToken);
        if (ctx.State == WorkflowState.Failed) return ctx;

        ctx = await BookFlightStepAsync(ctx, cancellationToken);
        if (ctx.State == WorkflowState.Failed) return ctx;

        return await ReadItineraryStepAsync(ctx, cancellationToken);
    }

    private async Task<BookingWorkflowContext> SearchFlightsStepAsync(
        BookingWorkflowContext ctx, CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = ctx.Origin,
                ["destination"] = ctx.Destination,
                ["departureDate"] = ctx.DepartureDate,
                ["passengerCount"] = 1
            },
            cancellationToken: cancellationToken);

        if (result.IsError == true)
            return ctx with
            {
                State = WorkflowState.Failed,
                ErrorMessage = ((TextContentBlock)result.Content[0]).Text
            };

        // Select the first available flight — real implementations apply business rules here.
        var flights = JsonSerializer.Deserialize<FlightSearchResult>(
            ((TextContentBlock)result.Content[0]).Text);

        var first = flights?.Options.FirstOrDefault();
        if (first is null)
            return ctx with { State = WorkflowState.Failed, ErrorMessage = "No flights available." };

        return ctx with
        {
            SelectedFlightId = first.FlightId,
            State = WorkflowState.FlightSelected
        };
    }

    private async Task<BookingWorkflowContext> BookFlightStepAsync(
        BookingWorkflowContext ctx, CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "book_flight",
            new Dictionary<string, object?>
            {
                ["flightId"] = ctx.SelectedFlightId,
                ["idempotencyKey"] = ctx.CorrelationId,
                ["passengerName"] = ctx.PassengerName
            },
            cancellationToken: cancellationToken);

        if (result.IsError == true)
            return ctx with
            {
                State = WorkflowState.Failed,
                ErrorMessage = ((TextContentBlock)result.Content[0]).Text
            };

        var confirmation = JsonSerializer.Deserialize<BookingConfirmation>(
            ((TextContentBlock)result.Content[0]).Text);

        return ctx with
        {
            BookingReference = confirmation?.BookingReference,
            BookedAt = DateTimeOffset.UtcNow,
            State = WorkflowState.FlightBooked
        };
    }

    private async Task<BookingWorkflowContext> ReadItineraryStepAsync(
        BookingWorkflowContext ctx, CancellationToken cancellationToken)
    {
        var resource = await _client.ReadResourceAsync(
            "travel://itineraries/{bookingId}",
            new Dictionary<string, object?> { ["bookingId"] = ctx.BookingReference },
            cancellationToken: cancellationToken);

        var itineraryJson = resource.Contents.Count > 0 &&
            resource.Contents[0] is TextResourceContents text
            ? text.Text : null;

        return ctx with
        {
            ItineraryJson = itineraryJson,
            State = WorkflowState.Confirmed
        };
    }
}
