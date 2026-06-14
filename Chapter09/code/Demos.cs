// Chapter 9 — runnable adaptations of the chapter snippets.
//
// OfflineRetryQueue (ch09_11): persists pending operations and replays them
//   when connectivity is restored, with exponential back-off.
// CachingClient (ch09_9): wraps an upstream tool call with a simple in-memory
//   cache keyed by (tool, args) and a TTL.

using System.Collections.Concurrent;

namespace TravelBooking.Chapter09;

// ---------------------------------------------------------------------------
// ch09_11 — Offline retry queue
// ---------------------------------------------------------------------------
public sealed record PendingOperation(
    string Id, string ToolName, string Payload, int AttemptCount);

public sealed class OfflineRetryQueue
{
    private readonly ConcurrentQueue<PendingOperation> _queue = new();
    private readonly int _maxAttempts;

    public OfflineRetryQueue(int maxAttempts = 5) => _maxAttempts = maxAttempts;

    public int Pending => _queue.Count;

    public void Enqueue(string toolName, string payload)
        => _queue.Enqueue(new PendingOperation(Guid.NewGuid().ToString("N")[..8],
                                              toolName, payload, 0));

    public async Task<DrainResult> DrainAsync(
        Func<PendingOperation, CancellationToken, Task<bool>> dispatcher,
        CancellationToken cancellationToken = default)
    {
        var succeeded = 0;
        var dropped = 0;
        var requeued = new List<PendingOperation>();

        while (_queue.TryDequeue(out var op))
        {
            try
            {
                var ok = await dispatcher(op, cancellationToken).ConfigureAwait(false);
                if (ok) { succeeded++; continue; }
                throw new InvalidOperationException("dispatcher returned false");
            }
            catch
            {
                var next = op with { AttemptCount = op.AttemptCount + 1 };
                if (next.AttemptCount >= _maxAttempts) { dropped++; continue; }
                requeued.Add(next);
            }
        }

        foreach (var op in requeued) _queue.Enqueue(op);
        return new DrainResult(succeeded, dropped, requeued.Count);
    }
}

public sealed record DrainResult(int Succeeded, int Dropped, int Requeued);

// ---------------------------------------------------------------------------
// ch09_9 — Caching MCP client wrapper
// ---------------------------------------------------------------------------
public sealed class CachingClient<TArgs, TResult> where TArgs : notnull
{
    private sealed record Entry(TResult Value, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _cache = new();
    private readonly TimeSpan _ttl;
    private readonly Func<DateTimeOffset> _clock;
    private long _hits;
    private long _misses;

    public CachingClient(TimeSpan ttl, Func<DateTimeOffset>? clock = null)
    {
        _ttl = ttl;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public long Hits => Interlocked.Read(ref _hits);
    public long Misses => Interlocked.Read(ref _misses);

    public async Task<TResult> InvokeAsync(
        TArgs args,
        Func<TArgs, CancellationToken, Task<TResult>> upstream,
        CancellationToken cancellationToken = default)
    {
        var key = args.ToString() ?? "<null>";
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > _clock())
        {
            Interlocked.Increment(ref _hits);
            return entry.Value;
        }

        Interlocked.Increment(ref _misses);
        var value = await upstream(args, cancellationToken).ConfigureAwait(false);
        _cache[key] = new Entry(value, _clock() + _ttl);
        return value;
    }
}
