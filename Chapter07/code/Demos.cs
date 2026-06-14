// Chapter 7 — runnable adaptations of the reference snippets.
//
// These types are simplified, self-contained versions of patterns from the
// chapter that compile against only the BCL + ModelContextProtocol. They are
// the actual code executed by Program.cs; the verbatim ch07_*.cs snippets
// remain in this folder as reading material (excluded from compilation).

using System.Collections.Concurrent;
using System.Diagnostics;

namespace TravelBooking.Chapter07;

// ---------------------------------------------------------------------------
// ch07_6 — Session state cache (sliding TTL, per-session bag of values).
// ---------------------------------------------------------------------------
public sealed class SessionStateCache
{
    private sealed record Entry(object Value, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _store = new();
    private readonly TimeSpan _slidingTtl;
    private readonly Func<DateTimeOffset> _clock;

    public SessionStateCache(TimeSpan slidingTtl, Func<DateTimeOffset>? clock = null)
    {
        _slidingTtl = slidingTtl;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public void Set<T>(string sessionId, string key, T value) where T : notnull
        => _store[$"{sessionId}:{key}"] = new Entry(value, _clock() + _slidingTtl);

    public bool TryGet<T>(string sessionId, string key, out T? value)
    {
        var full = $"{sessionId}:{key}";
        if (_store.TryGetValue(full, out var entry) && entry.ExpiresAt > _clock())
        {
            // Sliding expiration: refresh on read.
            _store[full] = entry with { ExpiresAt = _clock() + _slidingTtl };
            value = (T)entry.Value;
            return true;
        }

        _store.TryRemove(full, out _);
        value = default;
        return false;
    }

    public int PurgeExpired()
    {
        var now = _clock();
        var removed = 0;
        foreach (var kv in _store)
        {
            if (kv.Value.ExpiresAt <= now && _store.TryRemove(kv.Key, out _))
                removed++;
        }
        return removed;
    }

    public int Count => _store.Count;
}

// ---------------------------------------------------------------------------
// ch07_7 — Polly-style resilience pipeline (retry + circuit breaker + timeout)
// implemented without the Polly dependency so the demo stays self-contained.
// ---------------------------------------------------------------------------
public sealed class ResiliencePipeline
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _attemptTimeout;
    private readonly int _breakAfterFailures;
    private readonly TimeSpan _breakDuration;

    private int _consecutiveFailures;
    private DateTimeOffset? _openUntil;

    public ResiliencePipeline(int maxAttempts, TimeSpan attemptTimeout,
                              int breakAfterFailures, TimeSpan breakDuration)
    {
        _maxAttempts = maxAttempts;
        _attemptTimeout = attemptTimeout;
        _breakAfterFailures = breakAfterFailures;
        _breakDuration = breakDuration;
    }

    public CircuitState State =>
        _openUntil is { } open && open > DateTimeOffset.UtcNow
            ? CircuitState.Open
            : _consecutiveFailures > 0 ? CircuitState.HalfOpen : CircuitState.Closed;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
                                         CancellationToken cancellationToken = default)
    {
        if (State == CircuitState.Open)
            throw new InvalidOperationException(
                $"Circuit is open until {_openUntil:O}.");

        Exception? last = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_attemptTimeout);
            try
            {
                var result = await action(cts.Token).ConfigureAwait(false);
                _consecutiveFailures = 0;
                _openUntil = null;
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException oce
                                       || !cancellationToken.IsCancellationRequested)
            {
                last = ex;
                _consecutiveFailures++;
                if (_consecutiveFailures >= _breakAfterFailures)
                {
                    _openUntil = DateTimeOffset.UtcNow + _breakDuration;
                    throw new InvalidOperationException(
                        $"Circuit opened after {_consecutiveFailures} failures.", ex);
                }
                // Linear back-off so the demo is deterministic.
                await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt), cancellationToken)
                          .ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            $"All {_maxAttempts} attempts failed.", last);
    }
}

public enum CircuitState { Closed, HalfOpen, Open }

// ---------------------------------------------------------------------------
// Flaky service used to exercise the resilience pipeline.
// ---------------------------------------------------------------------------
public sealed class FlakyFlightService
{
    private int _calls;
    public int Calls => _calls;
    private readonly int _failuresBeforeSuccess;

    public FlakyFlightService(int failuresBeforeSuccess) =>
        _failuresBeforeSuccess = failuresBeforeSuccess;

    public Task<string> SearchAsync(string origin, string destination, CancellationToken ct)
    {
        var n = Interlocked.Increment(ref _calls);
        if (n <= _failuresBeforeSuccess)
            throw new HttpRequestException($"Simulated upstream 503 on call #{n}.");
        return Task.FromResult($"OK {origin}->{destination} on attempt #{n}");
    }
}
