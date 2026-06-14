// Chapter 7 — Section 7.3.3
// Distributed-cache session manager for BookingWorkflowContext.
// Persists context after every state transition so that a process restart
// can resume from the last completed state rather than re-executing all steps.
// 24-hour TTL matches the booking confirmation window in the FlightsServer.

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TravelBooking.Client.Workflow;

namespace TravelBooking.Client;

public sealed class WorkflowSessionStore
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public WorkflowSessionStore(IDistributedCache cache) => _cache = cache;

    public async Task SaveAsync(
        BookingWorkflowContext ctx,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(ctx, _jsonOptions);
        await _cache.SetStringAsync(
            CacheKey(ctx.CorrelationId),
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultTtl
            },
            cancellationToken);
    }

    public async Task<BookingWorkflowContext?> LoadAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(
            CacheKey(correlationId), cancellationToken);

        return json is null
            ? null
            : JsonSerializer.Deserialize<BookingWorkflowContext>(json);
    }

    // Refresh the TTL without modifying the context — call on heartbeat checks.
    public async Task RefreshAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
        => await _cache.RefreshAsync(CacheKey(correlationId), cancellationToken);

    public async Task DeleteAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
        => await _cache.RemoveAsync(CacheKey(correlationId), cancellationToken);

    private static string CacheKey(string correlationId) => $"booking:{correlationId}";
}

// Resume helper — loads context and skips already-completed steps.
// A FlightBooked context with a non-null BookingReference skips SearchFlights
// and BookFlight, running only ReadItinerary. Prevents duplicate bookings on restart.
public sealed class ResumableFlightBookingWorkflow
{
    private readonly FlightBookingWorkflow _workflow;
    private readonly WorkflowSessionStore _store;

    public ResumableFlightBookingWorkflow(
        FlightBookingWorkflow workflow, WorkflowSessionStore store)
    {
        _workflow = workflow;
        _store = store;
    }

    public async Task<BookingWorkflowContext> RunOrResumeAsync(
        string correlationId,
        BookingWorkflowContext? initial = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = await _store.LoadAsync(correlationId, cancellationToken)
            ?? initial
            ?? new BookingWorkflowContext { CorrelationId = correlationId };

        if (ctx.State is WorkflowState.Confirmed or WorkflowState.Failed)
            return ctx;

        ctx = await _workflow.RunAsync(ctx, cancellationToken);
        await _store.SaveAsync(ctx, cancellationToken);
        return ctx;
    }
}
