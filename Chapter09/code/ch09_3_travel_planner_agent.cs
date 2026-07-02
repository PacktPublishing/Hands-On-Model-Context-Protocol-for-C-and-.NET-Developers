// Chapter 9 (Replacement) — Section 9.2.2
// TravelPlannerAgent: translates a natural-language user request into a structured
// TravelPlan by asking the LLM to return JSON that matches the TravelPlan schema.
// Uses ChatResponseFormat.Json for reliable structured output.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace TravelBooking.Agentic;

public sealed class TravelPlannerAgent(
    IChatClient chatClient,
    McpClient mcpClient,
    ILogger<TravelPlannerAgent> logger)
{
    public async Task<TravelPlan> PlanAsync(
        string userRequest, CancellationToken ct = default)
    {
        var tools = await mcpClient.ListToolsAsync(ct);
        var toolSummary = string.Join("\n",
            tools.Select(t => $"- {t.Name}: {t.Description}"));
        var prompt = $"""
            Available tools:
            {toolSummary}

            Rules: book_flight and cancel_flight are
            IsReversible=false, RequiresApproval=true.
            All other tools are IsReversible=true.

            Produce a TravelPlan JSON for: {userRequest}
            """;
        var response = await chatClient.GetResponseAsync(
            [new(ChatRole.User, prompt)],
            new ChatOptions { ResponseFormat = ChatResponseFormat.Json },
            ct);
        return JsonSerializer.Deserialize<TravelPlan>(response.Text)!;
    }

    /// <summary>
    /// Validates the plan against the current server capability list.
    /// Throws <see cref="PlanValidationException"/> for the first violation found.
    /// </summary>
    public async Task ValidateAsync(
        TravelPlan plan, CancellationToken ct = default)
    {
        var tools = (await mcpClient.ListToolsAsync(ct))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var step in plan.Steps)
        {
            if (!tools.Contains(step.ToolName))
                throw new PlanValidationException(
                    $"Tool '{step.ToolName}' does not exist on the server.");

            if (!step.IsReversible && step.CompensationTool is not null
                && !tools.Contains(step.CompensationTool))
                throw new PlanValidationException(
                    $"CompensationTool '{step.CompensationTool}' " +
                    $"for step '{step.ToolName}' does not exist.");
        }

        // Irreversible steps must not appear before their predecessor reversible steps.
        var irreversibleIndex = Array.FindIndex(
            plan.Steps, s => !s.IsReversible);
        var lastReversibleAfterIrreversible = plan.Steps
            .Skip(irreversibleIndex + 1)
            .Any(s => s.IsReversible);

        if (irreversibleIndex >= 0 && lastReversibleAfterIrreversible)
            logger.LogWarning(
                "Plan has reversible steps after irreversible ones — review ordering.");
    }
}

public sealed class PlanValidationException(string message)
    : Exception(message);
