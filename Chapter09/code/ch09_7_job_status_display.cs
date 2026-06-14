// Chapter 9 — Section 9.2.3
// Blazor component that displays background job status and updates in real time.
// OnStatusChanged fires from a thread pool thread (BackgroundService context),
// so InvokeAsync(StateHasChanged) is required — direct StateHasChanged() is silently discarded.
// Unsubscription in Dispose() prevents a memory leak when the component is destroyed.

using Microsoft.AspNetCore.Components;

namespace TravelBooking.Blazor.Components;

public sealed partial class JobStatusDisplay : ComponentBase, IDisposable
{
    [Inject] private JobStatusStore StatusStore { get; set; } = null!;
    [Inject] private McpJobQueue Queue { get; set; } = null!;

    private IReadOnlyList<JobStatus> _jobs = [];

    protected override void OnInitialized()
    {
        _jobs = StatusStore.GetAll();
        StatusStore.OnStatusChanged += HandleUpdate;
    }

    private void HandleUpdate(JobStatus _)
    {
        _jobs = StatusStore.GetAll();
        // InvokeAsync marshals the call back to the component's synchronization context.
        InvokeAsync(StateHasChanged);
    }

    private async Task SubmitJobAsync(string userMessage)
    {
        var job = McpJobQueue.CreateJob(userMessage);
        StatusStore.Add(job.Id);
        await Queue.EnqueueAsync(job);
    }

    public void Dispose() =>
        StatusStore.OnStatusChanged -= HandleUpdate;
}

// ── Supporting types ──────────────────────────────────────────────────────────

public enum JobState { Queued, Running, Completed, Failed }

public sealed record JobStatus(
    string Id,
    JobState State,
    string? Result = null,
    DateTimeOffset? CompletedAt = null);

public sealed class JobStatusStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, JobStatus> _store = new();

    public event Action<JobStatus>? OnStatusChanged;

    public void Add(string jobId)
    {
        var status = new JobStatus(jobId, JobState.Queued);
        _store[jobId] = status;
    }

    public void Update(string jobId, JobState state, string? result = null)
    {
        var status = new JobStatus(
            jobId, state, result,
            state is JobState.Completed or JobState.Failed
                ? DateTimeOffset.UtcNow : null);
        _store[jobId] = status;
        OnStatusChanged?.Invoke(status);
    }

    public IReadOnlyList<JobStatus> GetAll() =>
        [.. _store.Values.OrderByDescending(j => j.CompletedAt)];
}
