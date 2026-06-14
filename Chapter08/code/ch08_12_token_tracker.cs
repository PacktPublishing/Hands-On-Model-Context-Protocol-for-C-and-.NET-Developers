// Chapter 8 — Section 8.4.2
// Token cost tracker for the planning loop.
// Accumulates InputTokenCount and OutputTokenCount across all LLM calls in a workflow.
// Call Add(response.Usage) after each GetResponseAsync; call EstimatedCost() after the loop.

using Microsoft.Extensions.AI;

namespace TravelBooking.Orchestration;

public sealed class TokenTracker
{
    private long _inputTokens;
    private long _outputTokens;
    private readonly Dictionary<string, long> _toolTokens = [];

    public void Add(UsageDetails? usage, string? toolName = null)
    {
        if (usage is null) return;

        var input  = usage.InputTokenCount  ?? 0;
        var output = usage.OutputTokenCount ?? 0;

        _inputTokens  += input;
        _outputTokens += output;

        if (toolName is not null)
        {
            _toolTokens.TryGetValue(toolName, out var existing);
            _toolTokens[toolName] = existing + input + output;
        }
    }

    public long TotalTokens => _inputTokens + _outputTokens;

    // pricePerMillionInput / pricePerMillionOutput in USD (e.g., 3.00 / 15.00 for claude-sonnet-4-6).
    public decimal EstimatedCost(decimal inputPer1M, decimal outputPer1M) =>
        _inputTokens  / 1_000_000m * inputPer1M +
        _outputTokens / 1_000_000m * outputPer1M;

    // Throws if TotalTokens exceeds the budget so the caller can break the loop early.
    public void EnforceBudget(long maxTokens)
    {
        if (TotalTokens > maxTokens)
            throw new InvalidOperationException(
                $"Token budget of {maxTokens:N0} exceeded " +
                $"(current: {TotalTokens:N0}). Stopping workflow.");
    }

    public IReadOnlyDictionary<string, long> TokensByTool => _toolTokens;

    public override string ToString() =>
        $"in={_inputTokens:N0} out={_outputTokens:N0} total={TotalTokens:N0}";
}
