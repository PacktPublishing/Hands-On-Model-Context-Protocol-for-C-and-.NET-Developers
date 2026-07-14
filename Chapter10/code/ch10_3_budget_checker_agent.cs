// Chapter 10 — Section 10.2
// BudgetCheckerAgent: validates itinerary cost against corporate policy.
// Uses CallToolAsync directly so IsError can be inspected and escalation raised
// before any LLM turn attempts to interpret a failed tool result.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.MultiAgent;

/// <summary>Structured cost breakdown returned by the check_budget tool.</summary>
public sealed record ItineraryCostModel(
    string SessionId,
    decimal TotalCost,
    string Currency,
    decimal PolicyLimit,
    bool WithinPolicy,
    string[]? Recommendations);

public sealed class FullBudgetCheckerAgent(
    McpClient budgetMcpClient,
    ILogger<FullBudgetCheckerAgent> logger) : ISpecialistAgent
{
    public string AgentId => "budget";

    public async Task<AgentResult> RunAsync(
        HandoffToken handoff, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Budget check for session {SessionId}", handoff.SessionId);

        // Primary check: is the overall cost within policy?
        var checkResult = await budgetMcpClient.CallToolAsync(
            "check_budget",
            new Dictionary<string, object?>
            {
                ["session_id"]    = handoff.SessionId,
                ["prior_context"] = handoff.PriorContext ?? string.Empty
            }, ct: ct);

        if (checkResult.IsError is true)
        {
            var errorText = checkResult.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()?.Text ?? "Budget check tool returned an error.";
            logger.LogWarning(
                "Budget check tool error for session {Id}: {Error}",
                handoff.SessionId, errorText);
            return new AgentResult(
                AgentId, errorText,
                RequiresEscalation: true,
                EscalationReason: errorText);
        }

        var resultText = checkResult.Content
            .OfType<TextContentBlock>()
            .FirstOrDefault()?.Text ?? "{}";

        var costModel = System.Text.Json.JsonSerializer
            .Deserialize<ItineraryCostModel>(resultText);

        if (costModel is null)
            return new AgentResult(
                AgentId, "Could not parse budget check response.",
                RequiresEscalation: true,
                EscalationReason: "Budget tool returned unparseable JSON.");

        if (!costModel.WithinPolicy)
        {
            var reason =
                $"Itinerary cost {costModel.TotalCost:C} {costModel.Currency} " +
                $"exceeds the {costModel.PolicyLimit:C} policy limit. " +
                $"Recommendations: {string.Join("; ", costModel.Recommendations ?? [])}";
            logger.LogWarning(
                "Budget exceeded for session {Id}: {Reason}",
                handoff.SessionId, reason);
            return new AgentResult(
                AgentId, reason,
                RequiresEscalation: true,
                EscalationReason: reason);
        }

        logger.LogInformation(
            "Budget approved for session {Id}: {Cost:C}",
            handoff.SessionId, costModel.TotalCost);

        return new AgentResult(
            AgentId,
            $"Budget approved: {costModel.TotalCost:C} {costModel.Currency} " +
            $"is within the {costModel.PolicyLimit:C} limit.");
    }
}
