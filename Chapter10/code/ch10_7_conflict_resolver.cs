// Chapter 10 — Section 10.4.2
// ConflictResolver: attempts automated resolution when the budget checker rejects
// a planner output. Asks the planner to revise with a cost-reduction target,
// then re-validates. Escalates after two failed revision attempts.

namespace TravelBooking.MultiAgent;

public enum ConflictResolutionKind { NoConflict, Resolved, EscalateToUser }

public sealed record ConflictResolution(
    ConflictResolutionKind Kind,
    AgentResult? RevisedPlanResult = null,
    string? Reason = null)
{
    public static ConflictResolution NoConflict =>
        new(ConflictResolutionKind.NoConflict);

    public static ConflictResolution Resolved(AgentResult revised) =>
        new(ConflictResolutionKind.Resolved, revised);

    public static ConflictResolution EscalateToUser(string reason) =>
        new(ConflictResolutionKind.EscalateToUser, Reason: reason);
}

public sealed class ConflictResolver(
    ItineraryPlannerAgent planner,
    FullBudgetCheckerAgent budgetChecker,
    ILogger<ConflictResolver> logger)
{
    private const int MaxRevisionAttempts = 2;

    public async Task<ConflictResolution> ResolveAsync(
        AgentResult plannerResult,
        AgentResult budgetResult,
        CancellationToken ct = default)
    {
        if (!budgetResult.RequiresEscalation)
            return ConflictResolution.NoConflict;

        logger.LogInformation(
            "Attempting conflict resolution. Budget reason: {Reason}",
            budgetResult.EscalationReason);

        // Extract session ID from the budget result output (coordinator injects it).
        var sessionId = ExtractSessionId(budgetResult);

        for (var attempt = 1; attempt <= MaxRevisionAttempts; attempt++)
        {
            var revisionToken = new HandoffToken(
                sessionId,
                "planner",
                "Revise the itinerary to reduce total cost by 20 percent. " +
                "Prefer economy class substitutions on long-haul legs. " +
                "Keep the same destinations and approximate travel dates.",
                PriorContext: plannerResult.Output);

            var revisedPlan = await planner.RunAsync(revisionToken, ct);
            logger.LogInformation(
                "Revision attempt {N} completed", attempt);

            var recheckToken = new HandoffToken(
                sessionId,
                "budget",
                "Re-validate the revised itinerary",
                PriorContext: revisedPlan.Output);

            var recheck = await budgetChecker.RunAsync(recheckToken, ct);
            if (!recheck.RequiresEscalation)
            {
                logger.LogInformation(
                    "Conflict resolved on attempt {N}", attempt);
                return ConflictResolution.Resolved(revisedPlan);
            }

            logger.LogWarning(
                "Revision {N} still exceeds budget: {Reason}",
                attempt, recheck.EscalationReason);
            plannerResult = revisedPlan;
        }

        return ConflictResolution.EscalateToUser(
            $"Could not reduce cost within policy after {MaxRevisionAttempts} " +
            $"revision attempts. Last reason: {budgetResult.EscalationReason}");
    }

    private static string ExtractSessionId(AgentResult result)
    {
        // In production, parse from the structured output; use a placeholder here.
        _ = result;
        return "session_unknown";
    }
}
