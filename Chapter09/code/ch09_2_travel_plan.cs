// Chapter 9 (Replacement) — Section 9.2.1
// TravelPlan and WorkflowStep types shared between planner and executor.
// StepResult and ExecutionResult carry the executor's output back to the caller.

using System.Text.Json.Serialization;

namespace TravelBooking.Agentic;

// ---------------------------------------------------------------------------
// Plan types (planner output / executor input)
// ---------------------------------------------------------------------------

public sealed record WorkflowStep(
    string ToolName,
    IReadOnlyDictionary<string, object?> Args,
    bool RequiresApproval = false,
    bool IsReversible = true,
    string? CompensationTool = null);

public sealed record TravelPlan(
    string Intent,
    WorkflowStep[] Steps,
    string SuccessCriteria,
    string? UserPreferences = null);

// ---------------------------------------------------------------------------
// Execution result types (executor output)
// ---------------------------------------------------------------------------

public enum ExecutionOutcome
{
    Completed,
    Cancelled,
    Failed,
    Expired,
    AlreadyTerminated,
    InvalidState,
    BudgetExceeded
}

public sealed record StepResult(
    WorkflowStep Step,
    bool Succeeded,
    object? RawResult,
    string? ErrorMessage)
{
    public static StepResult Success(WorkflowStep step, object? raw) =>
        new(step, true, raw, null);

    public static StepResult Failure(WorkflowStep step, string error) =>
        new(step, false, null, error);
}

public sealed record ExecutionResult(
    ExecutionOutcome Outcome,
    IReadOnlyList<StepResult> Steps,
    string? BookingRef = null,
    string? ErrorMessage = null)
{
    public static ExecutionResult Completed(string bookingRef) =>
        new(ExecutionOutcome.Completed, [], bookingRef);

    public static ExecutionResult Completed(IReadOnlyList<StepResult> steps) =>
        new(ExecutionOutcome.Completed, steps);

    public static ExecutionResult Cancelled(IReadOnlyList<StepResult> steps) =>
        new(ExecutionOutcome.Cancelled, steps);

    public static ExecutionResult Failed(
        WorkflowStep step, string error, IReadOnlyList<StepResult> steps) =>
        new(ExecutionOutcome.Failed, steps, null, error);

    public static ExecutionResult Failed(
        string context, string error, IReadOnlyList<StepResult> steps) =>
        new(ExecutionOutcome.Failed, steps, null, $"{context}: {error}");

    public static ExecutionResult Expired(string message) =>
        new(ExecutionOutcome.Expired, [], null, message);

    public static ExecutionResult AlreadyTerminated(string workflowId) =>
        new(ExecutionOutcome.AlreadyTerminated, [],
            null, $"Workflow {workflowId} has already terminated.");

    public static ExecutionResult InvalidState(string stateName) =>
        new(ExecutionOutcome.InvalidState, [],
            null, $"Cannot resume workflow in state '{stateName}'.");
}
