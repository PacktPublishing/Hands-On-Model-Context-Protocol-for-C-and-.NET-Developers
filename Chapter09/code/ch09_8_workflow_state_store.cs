// Chapter 9 (Replacement) — Section 9.4.1
// WorkflowStateStore: persists and loads workflow state using IDistributedCache.
// Uses AOT-compatible source-generated JSON serialization for the WorkflowState hierarchy.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace TravelBooking.Agentic;

// Source-generated JSON context for the WorkflowState hierarchy.
// Register via: builder.Services.AddSingleton(WorkflowStateJsonContext.Default);
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(WorkflowState))]
[JsonSerializable(typeof(IdleState))]
[JsonSerializable(typeof(SearchingState))]
[JsonSerializable(typeof(ComparingState))]
[JsonSerializable(typeof(ReservingState))]
[JsonSerializable(typeof(AwaitingApprovalState))]
[JsonSerializable(typeof(ConfirmedState))]
[JsonSerializable(typeof(FailedState))]
[JsonSerializable(typeof(CancelledState))]
[JsonSerializable(typeof(FlightOption))]
internal partial class WorkflowStateJsonContext : JsonSerializerContext;

public sealed class WorkflowStateStore(
    IDistributedCache cache,
    IOptions<WorkflowOptions> opts)
{
    private static string CacheKey(string id) => $"wf:{id}";

    public async Task TransitionAsync(
        string workflowId,
        WorkflowState newState,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(newState,
            WorkflowStateJsonContext.Default.WorkflowState);
        await cache.SetStringAsync(
            CacheKey(workflowId), json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = opts.Value.StateTtl
            }, ct);
    }

    public async Task<WorkflowState?> LoadAsync(
        string workflowId, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(CacheKey(workflowId), ct);
        return json is null
            ? null
            : JsonSerializer.Deserialize<WorkflowState>(json,
                WorkflowStateJsonContext.Default.WorkflowState);
    }

    public async Task DeleteAsync(string workflowId, CancellationToken ct = default) =>
        await cache.RemoveAsync(CacheKey(workflowId), ct);
}

public sealed class WorkflowOptions
{
    /// <summary>
    /// How long to retain workflow state after the last transition.
    /// Must exceed the maximum reservation hold window plus a safety buffer.
    /// </summary>
    public TimeSpan StateTtl { get; init; } = TimeSpan.FromHours(2);
}
