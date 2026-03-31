// Chapter 2 - Section 2.3.2
// Dynamic resource handler for the itinerary://booking/{reference} URI template.
// Returns the full itinerary as JSON.
// NOTE: This is a simplified version - full resource implementation requires MCP server hosting

using System.ComponentModel;
using System.Text.Json;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

/// <summary>
/// Demonstrates resource handler concept for itinerary retrieval
/// In a full MCP server, this would be decorated with [McpServerResource("itinerary://booking/{reference}")]
/// </summary>
public sealed class ItineraryResourceHandler(IItineraryService itineraryService)
{
    [Description("Returns the full itinerary for a booking reference as JSON.")]
    public async Task<string> GetItineraryAsync(
        [Description("The booking reference returned by BookFlight, e.g. BK-SAMPLE123.")]
        string reference,

        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Booking reference must not be empty.", nameof(reference));

        var itinerary = await itineraryService.GetAsync(reference, ct);

        if (itinerary is null)
            throw new InvalidOperationException($"No itinerary found for reference '{reference}'.");

        return JsonSerializer.Serialize(itinerary);
    }
}
