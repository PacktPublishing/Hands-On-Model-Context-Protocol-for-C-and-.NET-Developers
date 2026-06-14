// Chapter 12 — Section 12.2.1
// Application-level ActivitySource instrumentation for distributed tracing.
// The SDK's "Experimental.ModelContextProtocol" ActivitySource creates a span
// automatically for the MCP protocol exchange. This file adds a child span
// around the downstream airline API call within the tool handler, attributing
// latency to the specific downstream dependency.
// LlmDecisionEnricher adds LLM reasoning metadata to the orchestrator span.

using System.Diagnostics;

namespace TravelBooking.Flights.Telemetry;

public sealed class FlightSearchTracing
{
    // One ActivitySource per logical service boundary.
    private static readonly ActivitySource Source =
        new("TravelBooking.Flights", "1.0.0");

    public async Task<T> TraceAirlineApiCallAsync<T>(
        string origin, string destination,
        Func<Task<T>> apiCall)
    {
        using var activity = Source.StartActivity(
            "airline-api.search", ActivityKind.Client);

        activity?.SetTag("airline.origin", origin);
        activity?.SetTag("airline.destination", destination);

        try
        {
            var result = await apiCall();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}

// Enriches the orchestrator's span with LLM decision metadata so that
// a trace viewer shows which tools the LLM selected and why.
public static class LlmDecisionEnricher
{
    public static void Enrich(
        Activity? activity,
        string model,
        IEnumerable<string> selectedTools,
        long promptTokens,
        long completionTokens,
        string? reasoning = null)
    {
        activity?.SetTag("llm.model", model);
        activity?.SetTag("llm.selected_tools", string.Join(",", selectedTools));
        activity?.SetTag("llm.prompt_tokens", promptTokens);
        activity?.SetTag("llm.completion_tokens", completionTokens);
        if (reasoning is not null)
            activity?.SetTag("llm.reasoning", reasoning);
    }
}
