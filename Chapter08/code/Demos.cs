// Chapter 8 — runnable adaptations of the chapter snippets.
//
// LoopDetector (ch08_2): catches repeated tool calls inside an orchestration
// loop so the agent stops invoking the same tool with the same arguments.
// AuditLogger (ch08_9): structured audit log of every decision the
// orchestrator makes, with monotonically increasing sequence numbers.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TravelBooking.Chapter08;

// ---------------------------------------------------------------------------
// ch08_2 — Loop detector
// ---------------------------------------------------------------------------
public sealed class LoopDetector
{
    private readonly int _maxRepeats;
    private readonly Dictionary<string, int> _counts = new();

    public LoopDetector(int maxRepeats = 3) => _maxRepeats = maxRepeats;

    public bool RegisterCall(string toolName, object? arguments)
    {
        var fingerprint = Fingerprint(toolName, arguments);
        var next = _counts.TryGetValue(fingerprint, out var n) ? n + 1 : 1;
        _counts[fingerprint] = next;
        return next > _maxRepeats;
    }

    public int DistinctCalls => _counts.Count;

    public int RepeatCount(string toolName, object? arguments) =>
        _counts.TryGetValue(Fingerprint(toolName, arguments), out var n) ? n : 0;

    private static string Fingerprint(string tool, object? args)
    {
        var json = args is null ? "null" : JsonSerializer.Serialize(args);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{tool}|{json}"));
        return Convert.ToHexString(hash, 0, 8);
    }
}

// ---------------------------------------------------------------------------
// ch08_9 — Audit logger
// ---------------------------------------------------------------------------
public sealed record AuditRecord(
    long Sequence,
    DateTimeOffset Timestamp,
    string Actor,
    string Action,
    IReadOnlyDictionary<string, string> Metadata);

public sealed class AuditLogger
{
    private long _sequence;
    private readonly ConcurrentQueue<AuditRecord> _records = new();
    private readonly Func<DateTimeOffset> _clock;

    public AuditLogger(Func<DateTimeOffset>? clock = null)
        => _clock = clock ?? (() => DateTimeOffset.UtcNow);

    public AuditRecord Log(string actor, string action, IDictionary<string, string>? metadata = null)
    {
        var rec = new AuditRecord(
            Sequence: Interlocked.Increment(ref _sequence),
            Timestamp: _clock(),
            Actor: actor,
            Action: action,
            Metadata: new Dictionary<string, string>(
                metadata ?? new Dictionary<string, string>()));
        _records.Enqueue(rec);
        return rec;
    }

    public IReadOnlyList<AuditRecord> Snapshot() => _records.ToArray();
}
