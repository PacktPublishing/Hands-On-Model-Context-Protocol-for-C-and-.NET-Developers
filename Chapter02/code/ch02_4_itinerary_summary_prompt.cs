// Chapter 2 - Section 2.3.3
// ItinerarySummaryPrompt: a two-message prompt that fetches booking data and injects it into
// an LLM context. Returns ChatMessage collection for the model to summarise.

using ModelContextProtocol.Server;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

[McpServerPromptType]
public sealed class ItinerarySummaryPrompt(IItineraryService itineraryService)
{
    [McpServerPrompt, Description("Generates a structured prompt that asks the LLM to summarise a travel itinerary.")]
    public async Task<ChatMessage[]> ItinerarySummaryAsync(
        [Description("The booking reference for the itinerary to summarise, e.g. BK-SAMPLE123.")]
        string reference,

        CancellationToken ct = default)
    {
        var itinerary = await itineraryService.GetAsync(reference, ct);

        if (itinerary is null)
            throw new InvalidOperationException($"No itinerary found for reference '{reference}'.");

        var json = JsonSerializer.Serialize(itinerary);

        return
        [
            new ChatMessage(ChatRole.System,
                "You are a travel assistant. Summarise the following itinerary in plain English, " +
                "highlighting departure times, total cost, and current status."),
            new ChatMessage(ChatRole.User,
                $"Itinerary data:\n{json}")
        ];
    }
}
