// Chapter 15 -- runnable adaptations of the chapter snippets.
//
// The verbatim ch15_*.cs files depend on external infrastructure -- OpenTelemetry
// exporters, Redis / IDistributedCache, ASP.NET Core middleware, Aspire, and Azure
// SDKs -- so they cannot compile together as a single console project. The types
// below distil the same ideas into self-contained code that compiles against only
// the BCL, and Program.cs exercises each one end-to-end.
//
// TokenUsageTracker    (ch15_6): aggregates prompt/completion/cached tokens via
//                                System.Diagnostics.Metrics so the same counters
//                                are publishable to OTLP.
// BudgetCapEnforcer    (ch15_7): per-workflow and per-tenant token caps.
// LlmResponseKeyBuilder(ch15_8): normalised prompt hashing + scope-based TTLs.
// ConsistentHashRouter (ch15_9): SHA-256 + virtual nodes ring router.
// LoadShedder          (ch15_10): priority-based load shedding decision.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;

namespace TravelBooking.Chapter15;

// ---------------------------------------------------------------------------
// ch15_6 -- Token usage tracker
// ---------------------------------------------------------------------------
public sealed class TokenUsageTracker : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _prompt;
    private readonly Counter<long> _completion;
    private readonly Counter<long> _cached;

    private long _promptTotal;
    private long _completionTotal;
    private long _cachedTotal;

    public TokenUsageTracker(IMeterFactory? meterFactory = null)
    {
        _meter = meterFactory?.Create("TravelBooking.Orchestrator")
                 ?? new Meter("TravelBooking.Orchestrator");
        _prompt     = _meter.CreateCounter<long>("mcp.llm.tokens.prompt",     unit: "tokens");
        _completion = _meter.CreateCounter<long>("mcp.llm.tokens.completion", unit: "tokens");
        _cached     = _meter.CreateCounter<long>("mcp.llm.tokens.cached",     unit: "tokens");
    }

    public void Record(string tenantId, long prompt, long completion, long cached = 0)
    {
        var tag = new KeyValuePair<string, object?>("tenant.id", tenantId);
        _prompt.Add(prompt, tag);
        _completion.Add(completion, tag);
        _cached.Add(cached, tag);
        Interlocked.Add(ref _promptTotal, prompt);
        Interlocked.Add(ref _completionTotal, completion);
        Interlocked.Add(ref _cachedTotal, cached);
    }

    public long PromptTotal => Interlocked.Read(ref _promptTotal);
    public long CompletionTotal => Interlocked.Read(ref _completionTotal);
    public long CachedTotal => Interlocked.Read(ref _cachedTotal);

    public void Dispose() => _meter.Dispose();
}

// ---------------------------------------------------------------------------
// ch15_7 -- Budget cap enforcer
// ---------------------------------------------------------------------------
public sealed class BudgetExceededException : Exception
{
    public BudgetExceededException(string message) : base(message) { }
}

public sealed class BudgetCapEnforcer
{
    private readonly ConcurrentDictionary<string, long> _workflow = new();
    private readonly ConcurrentDictionary<string, long> _period   = new();

    public long WorkflowCapTokens { get; set; } = 5_000;
    public long TenantPeriodCapTokens { get; set; } = 500_000;

    public void Enforce(string workflowId, string tenantId, long tokens)
    {
        var w = _workflow.AddOrUpdate(workflowId, tokens, (_, p) => p + tokens);
        if (w > WorkflowCapTokens)
            throw new BudgetExceededException(
                $"Workflow {workflowId} exceeded {WorkflowCapTokens:N0}-token cap (used {w:N0}).");

        var t = _period.AddOrUpdate(tenantId, tokens, (_, p) => p + tokens);
        if (t > TenantPeriodCapTokens)
            throw new BudgetExceededException(
                $"Tenant {tenantId} exceeded hourly {TenantPeriodCapTokens:N0}-token budget.");
    }

    public void ResetPeriodCounters() => _period.Clear();
    public void CompleteWorkflow(string workflowId) => _workflow.TryRemove(workflowId, out _);
}

// ---------------------------------------------------------------------------
// ch15_8 -- LLM response cache key builder (normalised prompt + scope TTL)
// ---------------------------------------------------------------------------
public sealed class LlmResponseKeyBuilder
{
    private static readonly Dictionary<string, TimeSpan> TtlByScope = new()
    {
        ["tool-schema"]   = TimeSpan.FromHours(24),
        ["static-policy"] = TimeSpan.FromHours(6),
        ["availability"]  = TimeSpan.FromMinutes(5),
        ["price-quote"]   = TimeSpan.FromMinutes(2),
    };

    public TimeSpan TtlFor(string scope) =>
        TtlByScope.GetValueOrDefault(scope, TimeSpan.FromMinutes(5));

    public string BuildCacheKey(string prompt, string scope)
    {
        var normalised = NormalisePrompt(prompt);
        var hash       = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return $"llm:{scope}:{Convert.ToHexString(hash)}";
    }

    // Normalisation strips whitespace variations so equivalent prompts share a key.
    private static string NormalisePrompt(string prompt) =>
        string.Join(' ', prompt.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

// ---------------------------------------------------------------------------
// ch15_9 -- Consistent hash router
// ---------------------------------------------------------------------------
public sealed class ConsistentHashRouter
{
    private readonly SortedDictionary<uint, string> _ring = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private const int VirtualNodesPerServer = 150;

    public void AddServer(string address)
    {
        _lock.EnterWriteLock();
        try { for (var i = 0; i < VirtualNodesPerServer; i++) _ring[Hash($"{address}:{i}")] = address; }
        finally { _lock.ExitWriteLock(); }
    }

    public void RemoveServer(string address)
    {
        _lock.EnterWriteLock();
        try { for (var i = 0; i < VirtualNodesPerServer; i++) _ring.Remove(Hash($"{address}:{i}")); }
        finally { _lock.ExitWriteLock(); }
    }

    public int ServerCount
    {
        get
        {
            _lock.EnterReadLock();
            try { return _ring.Values.Distinct().Count(); }
            finally { _lock.ExitReadLock(); }
        }
    }

    public string Route(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_ring.Count == 0) throw new InvalidOperationException("No servers registered.");
            var hash = Hash(key);
            foreach (var (position, server) in _ring)
                if (position >= hash) return server;
            return _ring.First().Value;
        }
        finally { _lock.ExitReadLock(); }
    }

    private static uint Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToUInt32(bytes, 0);
    }
}

// ---------------------------------------------------------------------------
// ch15_10 -- Priority-based load shedding decision
// ---------------------------------------------------------------------------
public enum RequestPriority { Critical, High, Standard, BestEffort }

public sealed class LoadShedder
{
    public int Tier1Threshold { get; set; } = 200; // Shed BestEffort above 200
    public int Tier2Threshold { get; set; } = 400; // Shed Standard above 400
    public int Tier3Threshold { get; set; } = 600; // Shed High above 600
    // Critical: never shed

    public bool ShouldShed(RequestPriority priority, int activeRequests) =>
        priority switch
        {
            RequestPriority.BestEffort => activeRequests > Tier1Threshold,
            RequestPriority.Standard   => activeRequests > Tier2Threshold,
            RequestPriority.High       => activeRequests > Tier3Threshold,
            RequestPriority.Critical   => false,
            _                          => false
        };

    public static RequestPriority Classify(string path)
    {
        if (path.StartsWith("/mcp/tools/process_payment", StringComparison.Ordinal) ||
            path.StartsWith("/mcp/tools/book_flight", StringComparison.Ordinal))
            return RequestPriority.Critical;

        if (path.StartsWith("/mcp/tools/book_hotel", StringComparison.Ordinal))
            return RequestPriority.High;

        if (path.StartsWith("/mcp", StringComparison.Ordinal))
            return RequestPriority.Standard;

        return RequestPriority.BestEffort;
    }
}
