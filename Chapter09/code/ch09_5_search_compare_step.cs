// Chapter 9 (Replacement) — Section 9.3.1
// SearchAndCompareStepHandler: calls search_flights, stores results in ComparingState,
// then uses the LLM to select the best option based on user preferences.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace TravelBooking.Agentic;

public sealed class SearchAndCompareStepHandler(
    McpClient mcpClient,
    IChatClient chatClient,
    WorkflowStateStore stateStore,
    ILogger<SearchAndCompareStepHandler> logger)
{
    public async Task<FlightOption> ExecuteAsync(
        string workflowId,
        WorkflowStep searchStep,
        string userPreferences,
        CancellationToken ct = default)
    {
        // Execute the search tool
        var tools = (await mcpClient.ListToolsAsync(ct))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);
        var searchTool = tools["search_flights"];

        var rawResult = await searchTool.InvokeAsync(
            new Dictionary<string, object?>
            {
                ["origin"]         = searchStep.Args["origin"],
                ["destination"]    = searchStep.Args["destination"],
                ["departure_date"] = searchStep.Args["departure_date"]
            }, ct);

        var options = JsonSerializer.Deserialize<FlightOption[]>(
            rawResult?.ToString() ?? "[]")!;

        logger.LogInformation(
            "search_flights returned {Count} options", options.Length);

        // Persist results immediately before engaging the LLM for selection
        await stateStore.TransitionAsync(
            workflowId, new ComparingState(options, workflowId), ct);

        // Ask the LLM to select the best option based on user preferences
        var selectionPrompt = $"""
            User preferences: {userPreferences}
            Available flights: {JsonSerializer.Serialize(options)}
            Return the flightId of the best option as plain text only.
            """;

        var selectionResponse = await chatClient.GetResponseAsync(
            [new(ChatRole.User, selectionPrompt)], null, ct);

        var selectedId = selectionResponse.Text.Trim();
        var selected = options.FirstOrDefault(o => o.FlightId == selectedId)
            ?? options.OrderBy(o => o.Price).First();

        await stateStore.TransitionAsync(
            workflowId,
            new ReservingState(selected, workflowId + "_res"),
            ct);

        return selected;
    }
}
