// Chapter 9 (Replacement) — Section 9.3.2
// ApprovalCheckpoint: races the IApprovalProvider response against the reservation
// expiry deadline using Task.WhenAny. Transitions to CancelledState if the reservation
// expires before the user responds.

using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Agentic;

public sealed class ApprovalCheckpoint(
    IApprovalProvider approvals,
    WorkflowStateStore stateStore,
    ILogger<ApprovalCheckpoint> logger)
{
    public async Task<ApprovalResult> RequestWithDeadlineAsync(
        string workflowId,
        AwaitingApprovalState state,
        CancellationToken ct = default)
    {
        var approvalArgs = new Dictionary<string, object?>
        {
            ["reservation_id"] = state.ReservationId,
            ["flight"]         = state.Selected.FlightId,
            ["total_price"]    = state.TotalPrice,
            ["expires_at"]     = state.ReservationExpiry.ToString("O")
        };

        var approvalTask = approvals.RequestApprovalAsync(
            "book_flight", approvalArgs, ct);

        var remaining = state.ReservationExpiry - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            await ExpireAsync(workflowId, ct);
            return ApprovalResult.Expired;
        }

        var expiryTask = Task.Delay(remaining, ct);
        var winner = await Task.WhenAny(approvalTask, expiryTask);

        if (winner == expiryTask)
        {
            await ExpireAsync(workflowId, ct);
            return ApprovalResult.Expired;
        }

        var approved = await approvalTask;
        logger.LogInformation(
            "Approval for workflow {Id}: {Outcome}",
            workflowId, approved ? "approved" : "rejected");

        return approved ? ApprovalResult.Approved : ApprovalResult.Rejected;
    }

    private async Task ExpireAsync(string workflowId, CancellationToken ct)
    {
        logger.LogWarning("Reservation expired for workflow {Id}", workflowId);
        await stateStore.TransitionAsync(
            workflowId, new CancelledState("Reservation expired."), ct);
    }
}

public enum ApprovalResult { Approved, Rejected, Expired }
