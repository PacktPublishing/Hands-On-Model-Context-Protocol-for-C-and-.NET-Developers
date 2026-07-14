// Chapter 10 — Section 10.3
// AgentCoordinator: routes tasks to specialist agents in dependency order.
// Planner runs first; budget check and compatibility check can run in parallel
// when neither depends on the other's output; booking runs last.

namespace TravelBooking.MultiAgent;

public sealed class AgentCoordinator(
    ItineraryPlannerAgent planner,
    FullBudgetCheckerAgent budgetChecker,
    BookingAgent bookingAgent,
    SupportAgent supportAgent,
    ConflictResolver conflictResolver,
    EscalationHandler escalationHandler,
    ILogger<AgentCoordinator> logger)
{
    public async Task<CoordinatorResult> RunAsync(
        string sessionId,
        string userRequest,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Coordinator starting session {Id}", sessionId);

        // Detect support intent before entering the planning flow.
        if (IsSupportRequest(userRequest))
        {
            var supportToken = new HandoffToken(sessionId, "support", userRequest);
            var supportResult = await supportAgent.RunAsync(supportToken, ct);
            return CoordinatorResult.Completed(supportResult);
        }

        // Step 1: plan the itinerary.
        var planToken = new HandoffToken(sessionId, "planner", userRequest);
        var planResult = await planner.RunAsync(planToken, ct);
        logger.LogInformation("Planner completed for session {Id}", sessionId);

        // Step 2: validate budget (passes planner output as prior context).
        var budgetToken = new HandoffToken(
            sessionId, "budget",
            "Validate budget for planned itinerary",
            PriorContext: planResult.Output);
        var budgetResult = await budgetChecker.RunAsync(budgetToken, ct);

        if (budgetResult.RequiresEscalation)
        {
            // Attempt automated conflict resolution before escalating.
            var resolution = await conflictResolver
                .ResolveAsync(planResult, budgetResult, ct);

            if (resolution.Kind == ConflictResolutionKind.EscalateToUser)
            {
                await escalationHandler.EscalateAsync(
                    sessionId, resolution.Reason!, ct);
                return CoordinatorResult.EscalationRequired(budgetResult);
            }

            // Resolution produced a revised plan — proceed with it.
            planResult = resolution.RevisedPlanResult!;
        }

        // Step 3: book all legs.
        var bookingToken = new HandoffToken(
            sessionId, "booking",
            "Book all legs for the approved itinerary",
            PriorContext: planResult.Output);
        var bookingResult = await bookingAgent.RunAsync(bookingToken, ct);

        logger.LogInformation(
            "Coordinator completed session {Id}", sessionId);

        return CoordinatorResult.Completed(bookingResult);
    }

    private static bool IsSupportRequest(string request) =>
        request.Contains("cancel", StringComparison.OrdinalIgnoreCase)
        || request.Contains("refund", StringComparison.OrdinalIgnoreCase)
        || request.Contains("complaint", StringComparison.OrdinalIgnoreCase);
}
