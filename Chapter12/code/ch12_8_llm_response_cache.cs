// Chapter 12 — Section 12.3.3
// LLM response caching with normalised prompt hashing and scope-based TTLs.
// The cache key is a SHA-256 hash of the normalised prompt so minor formatting
// differences (whitespace, parameter order) do not cause unnecessary cache misses.
// TTLs are scoped to query type: tool-schema decisions are stable for 24 hours;
// price quotes expire in 2 minutes to limit staleness risk.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.AI;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TravelBooking.Orchestrator.Caching;

public sealed class LlmResponseCache(IDistributedCache cache)
{
    private static readonly Dictionary<string, TimeSpan> TtlByScope = new()
    {
        ["tool-schema"]   = TimeSpan.FromHours(24),  // Stable: which tools are available
        ["static-policy"] = TimeSpan.FromHours(6),   // Semi-stable: routing rules
        ["availability"]  = TimeSpan.FromMinutes(5), // Short-lived: seat/room availability
        ["price-quote"]   = TimeSpan.FromMinutes(2), // Very short: prices change rapidly
    };

    public async Task<ChatCompletion?> GetAsync(
        string prompt, string scope, CancellationToken ct = default)
    {
        var key  = BuildCacheKey(prompt, scope);
        var json = await cache.GetStringAsync(key, ct);
        return json is null ? null : JsonSerializer.Deserialize<ChatCompletion>(json);
    }

    public async Task SetAsync(
        ChatCompletion response, string prompt, string scope, CancellationToken ct = default)
    {
        var key = BuildCacheKey(prompt, scope);
        var ttl = TtlByScope.GetValueOrDefault(scope, TimeSpan.FromMinutes(5));
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);
    }

    private static string BuildCacheKey(string prompt, string scope)
    {
        var normalised = NormalisePrompt(prompt);
        var hash       = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return $"llm:{scope}:{Convert.ToHexString(hash)}";
    }

    // Normalisation strips whitespace variations so equivalent prompts share a cache key.
    private static string NormalisePrompt(string prompt) =>
        string.Join(' ', prompt.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
