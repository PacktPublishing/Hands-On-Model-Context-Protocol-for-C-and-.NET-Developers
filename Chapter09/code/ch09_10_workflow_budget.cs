// Chapter 9 (Replacement) — Section 9.5.1
// WorkflowBudget: imposes a hard upper limit on tool invocations per workflow execution.
// Throws BudgetExceededException before the tool call occurs when the budget is exhausted.

using System.Diagnostics.Metrics;

namespace TravelBooking.Agentic;

public sealed class WorkflowBudget
{
    private static readonly Meter Meter = new("TravelBooking.Agentic");

    private static readonly ObservableGauge<int> BudgetRemainingGauge =
        Meter.CreateObservableGauge<int>(
            "workflow.budget.remaining",
            description: "Tool call slots remaining in the current workflow execution");

    private readonly int _maxToolCalls;
    private int _calls;

    public WorkflowBudget(int maxToolCalls = 20)
    {
        _maxToolCalls = maxToolCalls;
        // Capture 'this' for the observable gauge callback.
        BudgetRemainingGauge.RecordObservableResult(_ =>
            new Measurement<int>(Remaining));
    }

    public int Remaining => Math.Max(0, _maxToolCalls - _calls);

    /// <summary>
    /// Increments the consumed call count. Throws before the tool call
    /// occurs so no side effects are produced when the budget is exhausted.
    /// </summary>
    public void Consume(string toolName)
    {
        var current = Interlocked.Increment(ref _calls);
        if (current > _maxToolCalls)
            throw new BudgetExceededException(
                $"Workflow halted: '{toolName}' would be call #{current}, " +
                $"exceeding the {_maxToolCalls}-call budget.");
    }

    /// <summary>
    /// Returns remaining budget as a string suitable for injection into an LLM
    /// system prompt so the planner can self-regulate its plan length.
    /// </summary>
    public string ToSystemPromptHint() =>
        $"Tool call budget: {Remaining} of {_maxToolCalls} remaining. " +
        "Plan only as many steps as required; avoid speculative calls.";
}

public sealed class BudgetExceededException(string message) : Exception(message);
