// Chapter 12 — Section 12.3.1
// Token usage tracking with multi-dimensional Counter attribution.
// Records prompt and completion tokens per LLM call, tagged with
// tool name, tenant ID, and session ID so cost can be attributed
// to specific capabilities, tenants, and user sessions.
// UsageDetails comes from Microsoft.Extensions.AI.

using Microsoft.Extensions.AI;
using System.Diagnostics.Metrics;

namespace TravelBooking.Orchestrator.Telemetry;

public sealed class TokenUsageTracker : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _promptTokens;
    private readonly Counter<long> _completionTokens;
    private readonly Counter<long> _cachedHits;

    public TokenUsageTracker(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("TravelBooking.Orchestrator");

        _promptTokens = _meter.CreateCounter<long>(
            "mcp.llm.tokens.prompt", unit: "tokens",
            description: "Prompt tokens consumed per LLM interaction");

        _completionTokens = _meter.CreateCounter<long>(
            "mcp.llm.tokens.completion", unit: "tokens",
            description: "Completion tokens produced per LLM interaction");

        // Tracks tokens that were served from cache rather than consumed from the LLM API.
        _cachedHits = _meter.CreateCounter<long>(
            "mcp.llm.tokens.cache_hit", unit: "tokens",
            description: "Tokens saved by LLM response cache hits");
    }

    public void Record(
        UsageDetails usage,
        string toolName,
        string tenantId,
        string sessionId,
        bool wasCacheHit = false)
    {
        var tags = new TagList
        {
            { "mcp.tool.name", toolName },
            { "tenant.id",     tenantId },
            { "session.id",    sessionId }
        };

        var promptCount     = usage.InputTokenCount  ?? 0;
        var completionCount = usage.OutputTokenCount ?? 0;

        if (wasCacheHit)
        {
            _cachedHits.Add(promptCount + completionCount, tags);
        }
        else
        {
            _promptTokens.Add(promptCount, tags);
            _completionTokens.Add(completionCount, tags);
        }
    }

    public void Dispose() => _meter.Dispose();
}
