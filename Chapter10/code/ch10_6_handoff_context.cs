// Chapter 10 — Section 10.4.1
// HandoffContextBuilder: serializes structured prior context for each coordinator
// transition so receiving agents get typed JSON rather than free-text summaries.

using System.Text.Json;

namespace TravelBooking.MultiAgent;

public static class HandoffContextBuilder
{
    private static readonly JsonSerializerOptions Options =
        new() { WriteIndented = false };

    /// <summary>
    /// Context passed from the planner to the budget checker.
    /// Includes flight count and an estimated cost so the budget tool
    /// can validate without re-parsing the full itinerary.
    /// </summary>
    public static string ForBudgetCheck(
        DraftItinerary itinerary, decimal estimatedCost) =>
        JsonSerializer.Serialize(new
        {
            itinerary_id  = itinerary.SessionId,
            flight_count  = itinerary.Legs.Length,
            total_cost    = estimatedCost,
            currency      = "GBP"
        }, Options);

    /// <summary>
    /// Context passed from the coordinator to the booking agent after
    /// the budget check has been approved.
    /// </summary>
    public static string ForBooking(
        DraftItinerary itinerary,
        decimal approvedBudget) =>
        JsonSerializer.Serialize(new
        {
            itinerary_id    = itinerary.SessionId,
            approved_budget = approvedBudget,
            leg_count       = itinerary.Legs.Length,
            legs            = itinerary.Legs
        }, Options);

    /// <summary>
    /// Context passed from the coordinator to the support agent for
    /// cancellation or refund requests.
    /// </summary>
    public static string ForSupport(
        string bookingReference,
        string requestType,
        string? additionalDetails = null) =>
        JsonSerializer.Serialize(new
        {
            booking_reference  = bookingReference,
            request_type       = requestType,
            additional_details = additionalDetails ?? string.Empty
        }, Options);
}
