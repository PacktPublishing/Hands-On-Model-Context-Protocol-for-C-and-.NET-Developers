// Chapter 9 — Section 9.3.1
// CachingMcpClient decorator that wraps McpClient with three-level SQLite caching.
// Level 1 — fresh hit: non-expired entry, server not contacted.
// Level 2 — server-first: online + stale/empty cache, result persisted after call.
// Level 3 — stale fallback: offline + expired entry, result returned with staleness marker.
// Write operations (book_flight, cancel_flight, process_payment) bypass the cache always.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.Blazor.Services;

public sealed class CachingMcpClient(
    McpClient inner,
    IToolResultCache cache,
    IConnectivityService connectivity)
{
    // Tools that must always reach the server — never cache write operations.
    private static readonly HashSet<string> NonCacheable =
        ["book_flight", "cancel_flight", "process_payment", "refund_payment"];

    // Per-tool TTL — tools absent from this map are not cached.
    private static readonly Dictionary<string, TimeSpan> Ttl = new()
    {
        ["search_flights"]  = TimeSpan.FromMinutes(15),
        ["search_hotels"]   = TimeSpan.FromMinutes(15),
        ["get_itinerary"]   = TimeSpan.FromMinutes(60)
    };

    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken ct = default)
    {
        if (NonCacheable.Contains(toolName) || !Ttl.ContainsKey(toolName))
            return await inner.CallToolAsync(toolName, args, ct);

        var key = ComputeKey(toolName, args);
        var entry = await cache.FindAsync(key, ct);

        // Level 1: fresh cache hit — skip the server entirely.
        if (entry is { IsExpired: false })
            return entry.Result;

        // Level 2: online — fetch from server and refresh the cache.
        if (connectivity.IsOnline)
        {
            var result = await inner.CallToolAsync(toolName, args, ct);
            await cache.UpsertAsync(key, result, Ttl[toolName], ct);
            return result;
        }

        // Level 3: offline with stale entry — serve with a staleness indicator.
        if (entry is not null)
            return entry.Result with
            {
                Content =
                [
                    ..entry.Result.Content,
                    new TextContentBlock
                    {
                        Text = "[cached — results may be outdated]"
                    }
                ]
            };

        throw new McpException(
            "Server unreachable and no cached data available for this search.");
    }

    private static string ComputeKey(string toolName, IReadOnlyDictionary<string, object?> args) =>
        $"{toolName}:{System.Text.Json.JsonSerializer.Serialize(args)}";
}

// Minimal cache contract — implement with EF Core SQLite for persistence.
public interface IToolResultCache
{
    Task<CacheEntry?> FindAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(string key, CallToolResult result, TimeSpan ttl, CancellationToken ct = default);
}

public sealed record CacheEntry(
    string Key,
    CallToolResult Result,
    DateTimeOffset ExpiresAt)
{
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}
