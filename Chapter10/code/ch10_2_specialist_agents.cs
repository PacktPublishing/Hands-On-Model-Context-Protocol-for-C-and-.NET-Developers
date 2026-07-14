// Chapter 10 — Section 10.2
// Four specialist agents: ItineraryPlannerAgent, BookingAgent, BudgetCheckerAgent,
// SupportAgent. Each owns its IChatClient, its McpClient, and its system prompt.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.MultiAgent;

/// <summary>
/// Plans multi-leg travel itineraries. Writes the draft to the shared MCP server
/// using the save_draft_itinerary tool so the BookingAgent can read it by URI.
/// </summary>
public sealed class ItineraryPlannerAgent(
    IChatClient chatClient,
    McpClient sharedMcpClient) : ISpecialistAgent
{
    public string AgentId => "planner";

    public async Task<AgentResult> RunAsync(
        HandoffToken handoff, CancellationToken ct = default)
    {
        var tools = await sharedMcpClient.ListToolsAsync(ct);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, handoff.Task)
        };
        var options = new ChatOptions { Tools = [.. tools] };
        var response = await chatClient.GetResponseAsync(messages, options, ct);
        return new AgentResult(AgentId, response.Text);
    }

    private static readonly string SystemPrompt = """
        You are a travel itinerary planner. When given a destination and travel dates,
        build a multi-leg itinerary and save it by calling save_draft_itinerary.
        Always include airline preference, cabin class, and connection time constraints.
        Do not make bookings — planning only.
        """;
}

/// <summary>
/// Reads the draft itinerary from the shared server and books each flight leg
/// in sequence. Uses ListToolsAsync each call to pick up seasonal capability changes.
/// </summary>
public sealed class BookingAgent(
    IChatClient chatClient,
    McpClient flightsMcpClient,
    McpClient sharedMcpClient) : ISpecialistAgent
{
    public string AgentId => "booking";

    public async Task<AgentResult> RunAsync(
        HandoffToken handoff, CancellationToken ct = default)
    {
        // Read the draft itinerary the planner saved to the shared server.
        var resource = await sharedMcpClient.ReadResourceAsync(
            $"itinerary://{handoff.SessionId}", ct);
        var itineraryJson = resource.Contents
            .OfType<TextResourceContents>()
            .FirstOrDefault()?.Text;
        if (string.IsNullOrEmpty(itineraryJson))
            return new AgentResult(AgentId,
                "No draft itinerary found. Run the planner first.");

        var tools = await flightsMcpClient.ListToolsAsync(ct);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,
                $"Book all legs in this itinerary:\n{itineraryJson}")
        };
        var response = await chatClient.GetResponseAsync(
            messages, new ChatOptions { Tools = [.. tools] }, ct);
        return new AgentResult(AgentId, response.Text);
    }

    private static readonly string SystemPrompt = """
        You are a flight booking specialist. Call search_flights before book_flight
        for each leg. Never invent flight IDs. Stop immediately if any booking fails.
        """;
}

/// <summary>
/// Checks the proposed itinerary cost against corporate policy limits.
/// Returns RequiresEscalation = true when the budget is exceeded and cannot be trimmed.
/// </summary>
public sealed class BudgetCheckerAgent(
    McpClient budgetMcpClient) : ISpecialistAgent
{
    public string AgentId => "budget";

    public async Task<AgentResult> RunAsync(
        HandoffToken handoff, CancellationToken ct = default)
    {
        var result = await budgetMcpClient.CallToolAsync(
            "check_budget",
            new Dictionary<string, object?>
            {
                ["session_id"]  = handoff.SessionId,
                ["prior_context"] = handoff.PriorContext ?? ""
            }, ct: ct);

        if (result.IsError is true)
        {
            var reason = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()?.Text ?? "Budget check failed.";
            return new AgentResult(
                AgentId, reason,
                RequiresEscalation: true,
                EscalationReason: reason);
        }

        var text = result.Content
            .OfType<TextContentBlock>()
            .FirstOrDefault()?.Text ?? "Budget approved.";
        return new AgentResult(AgentId, text);
    }
}

/// <summary>
/// Handles cancellation requests, refunds, and complaints.
/// Activated by the coordinator when intent classification identifies support topics.
/// </summary>
public sealed class SupportAgent(
    IChatClient chatClient,
    McpClient supportMcpClient) : ISpecialistAgent
{
    public string AgentId => "support";

    public async Task<AgentResult> RunAsync(
        HandoffToken handoff, CancellationToken ct = default)
    {
        var tools = await supportMcpClient.ListToolsAsync(ct);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, handoff.Task)
        };
        var response = await chatClient.GetResponseAsync(
            messages, new ChatOptions { Tools = [.. tools] }, ct);
        return new AgentResult(AgentId, response.Text);
    }

    private static readonly string SystemPrompt = """
        You are a customer support specialist for travel bookings.
        Handle cancellation requests, process refunds using process_refund,
        and log complaints with log_complaint. Always confirm booking reference
        before any refund action.
        """;
}
