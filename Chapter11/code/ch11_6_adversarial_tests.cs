// Chapter 11 — Section 11.4
// Adversarial test cases for the BookingAgent: ambiguous requests,
// unavailable flights, and policy violations. Each asserts a behavior class
// rather than exact output strings to remain robust across LLM updates.

using TravelBooking.MultiAgent;
using Xunit;

namespace TravelBooking.Testing;

/// <summary>
/// Classified behavior returned by the booking agent.
/// Determined by inspecting RequiresEscalation and the presence of
/// clarification intent markers in the output.
/// </summary>
public enum AgentBehavior
{
    CompletedBooking,
    Clarification,
    Escalation,
    NoResults
}

public static class BehaviorClassifier
{
    private static readonly string[] ClarificationMarkers =
        ["which destination", "please specify", "clarify", "more details"];

    private static readonly string[] NoResultsMarkers =
        ["no flights", "no available", "unavailable", "could not find"];

    public static AgentBehavior Classify(AgentResult result)
    {
        if (result.RequiresEscalation)
            return AgentBehavior.Escalation;

        var lower = result.Output.ToLowerInvariant();
        if (ClarificationMarkers.Any(m => lower.Contains(m)))
            return AgentBehavior.Clarification;
        if (NoResultsMarkers.Any(m => lower.Contains(m)))
            return AgentBehavior.NoResults;
        return AgentBehavior.CompletedBooking;
    }
}

/// <summary>
/// Adversarial scenario tests. Each test uses a dedicated stub server
/// configured to simulate the relevant environmental condition.
/// </summary>
public sealed class BookingAgentAdversarialTests
{
    [Theory]
    [InlineData("book the cheapest flight", AgentBehavior.Clarification)]
    [InlineData("I need to fly somewhere nice", AgentBehavior.Clarification)]
    public async Task AmbiguousRequest_RequestsClarification(
        string request, AgentBehavior expected)
    {
        await using var server =
            await InProcessTestServer.CreateAsync(
                [TravelStubs.SearchFlights(), TravelStubs.BookFlight()]);

        var agent = BuildBookingAgent(server.Client);
        var result = await agent.RunAsync(
            new HandoffToken("s1", "booking", request));

        Assert.Equal(expected, BehaviorClassifier.Classify(result));
    }

    [Fact]
    public async Task SearchReturnsNoFlights_ReportsNoResults()
    {
        // Stub returns empty array — no flights for this route.
        var emptySearchStub = McpServerTool.Create(
            (string origin, string destination, string date) => "[]",
            new McpServerToolCreateOptions { Name = "search_flights" });

        await using var server =
            await InProcessTestServer.CreateAsync([emptySearchStub]);

        var agent = BuildBookingAgent(server.Client);
        var result = await agent.RunAsync(
            new HandoffToken("s2", "booking",
                "Book a flight from LHR to XYZ on 2025-08-01"));

        Assert.Equal(AgentBehavior.NoResults, BehaviorClassifier.Classify(result));
    }

    [Fact]
    public async Task BookFlightPermanentError_Escalates()
    {
        await using var faultServer =
            await FaultInjectionServer.CreatePartialAsync(
                faults: new() { ["book_flight"] =
                    new("DUPLICATE_BOOKING:REF-999") },
                healthyTools: [TravelStubs.SearchFlights()]);

        var agent = BuildBookingAgent(faultServer.Client);
        var result = await agent.RunAsync(
            new HandoffToken("s3", "booking",
                "Book flight SU123 for John Smith"));

        Assert.Equal(AgentBehavior.Escalation, BehaviorClassifier.Classify(result));
        Assert.Contains("DUPLICATE_BOOKING", result.EscalationReason);
    }

    private static BookingAgent BuildBookingAgent(McpClient client) =>
        throw new NotImplementedException(
            "Wire a real IChatClient and sharedMcpClient in the test fixture.");
}
