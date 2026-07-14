// Chapter 10 — Section 10.3
// Shared MCP server resource patterns: DraftItinerary, FlightLeg, and reading
// the itinerary resource by URI. The planner calls save_draft_itinerary (a tool),
// the booking agent reads itinerary://{sessionId} (a resource).

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.MultiAgent;

/// <summary>A single flight leg within a draft itinerary.</summary>
public sealed record FlightLeg(
    string Origin,
    string Destination,
    string PreferredDate,
    string CabinClass,
    int MaxConnectionMinutes);

/// <summary>The draft itinerary persisted to the shared MCP server.</summary>
public sealed record DraftItinerary(
    string SessionId,
    FlightLeg[] Legs,
    string UserPreferences,
    DateTimeOffset CreatedAt);

/// <summary>
/// Reads the draft itinerary resource from the shared MCP server.
/// The URI scheme itinerary://{sessionId} is a template registered by the server.
/// </summary>
public sealed class SharedItineraryReader(McpClient sharedMcpClient)
{
    public async Task<DraftItinerary?> ReadAsync(
        string sessionId, CancellationToken ct = default)
    {
        var resource = await sharedMcpClient.ReadResourceAsync(
            $"itinerary://{sessionId}", ct);

        var json = resource.Contents
            .OfType<TextResourceContents>()
            .FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(json))
            return null;

        return System.Text.Json.JsonSerializer
            .Deserialize<DraftItinerary>(json);
    }
}

/// <summary>
/// Writes a draft itinerary to the shared MCP server by invoking
/// the save_draft_itinerary tool. The planner calls this after generating a plan.
/// </summary>
public sealed class SharedItineraryWriter(McpClient sharedMcpClient)
{
    public async Task SaveAsync(
        DraftItinerary itinerary, CancellationToken ct = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(itinerary);

        var result = await sharedMcpClient.CallToolAsync(
            "save_draft_itinerary",
            new Dictionary<string, object?>
            {
                ["session_id"] = itinerary.SessionId,
                ["itinerary"]  = json
            }, ct: ct);

        if (result.IsError is true)
        {
            var error = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()?.Text ?? "save_draft_itinerary failed.";
            throw new InvalidOperationException(
                $"Could not save draft itinerary: {error}");
        }
    }
}
