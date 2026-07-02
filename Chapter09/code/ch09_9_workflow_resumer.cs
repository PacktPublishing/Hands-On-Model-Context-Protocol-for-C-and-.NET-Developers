// Chapter 9 (Replacement) — Section 9.4.2
// WorkflowResumer: loads persisted state and re-enters the workflow at the correct point.
// FailedState retries from the failed step. AwaitingApprovalState re-presents the
// confirmation prompt with the remaining expiry time. Terminal states return immediately.

namespace TravelBooking.Agentic;

public sealed class WorkflowResumer(
    WorkflowStateStore stateStore,
    TravelExecutorAgent executor,
    ApprovalCheckpoint approvalCheckpoint,
    BookingStepHandler bookingStep,
    ILogger<WorkflowResumer> logger)
{
    public async Task<ExecutionResult> ResumeAsync(
        string workflowId, CancellationToken ct = default)
    {
        var state = await stateStore.LoadAsync(workflowId, ct)
            ?? throw new WorkflowNotFoundException(workflowId);

        logger.LogInformation(
            "Resuming workflow {Id} from state {State}",
            workflowId, state.GetType().Name);

        return state switch
        {
            FailedState failed =>
                await RetryFromFailureAsync(workflowId, failed, ct),
            AwaitingApprovalState approval =>
                await ResumeApprovalAsync(workflowId, approval, ct),
            ConfirmedState or CancelledState =>
                ExecutionResult.AlreadyTerminated(workflowId),
            _ =>
                ExecutionResult.InvalidState(state.GetType().Name)
        };
    }

    private async Task<ExecutionResult> RetryFromFailureAsync(
        string workflowId,
        FailedState failed,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Retrying from FailedState (previous: '{Reason}', attempts: {N})",
            failed.Reason, failed.AttemptCount);

        // Re-transition to Reserving so the executor can re-run the reservation.
        // Caller is responsible for supplying new payment details if required.
        await stateStore.TransitionAsync(
            workflowId,
            new ReservingState(
                new FlightOption("", "", "", "", default, default, 0, "", 0),
                workflowId + "_retry"),
            ct);

        // The caller must re-invoke the full executor with an updated plan
        // that starts from the ReservingState precondition.
        return ExecutionResult.InvalidState("retry_requires_new_plan");
    }

    private async Task<ExecutionResult> ResumeApprovalAsync(
        string workflowId,
        AwaitingApprovalState approval,
        CancellationToken ct)
    {
        var result = await approvalCheckpoint
            .RequestWithDeadlineAsync(workflowId, approval, ct);

        if (result != ApprovalResult.Approved)
            return ExecutionResult.Cancelled([]);

        return await bookingStep.ExecuteAsync(
            workflowId,
            approval.ReservationId,
            paymentToken: string.Empty,   // caller injects token
            ct);
    }
}

public sealed class WorkflowNotFoundException(string workflowId)
    : Exception($"Workflow '{workflowId}' was not found in the state store.");
