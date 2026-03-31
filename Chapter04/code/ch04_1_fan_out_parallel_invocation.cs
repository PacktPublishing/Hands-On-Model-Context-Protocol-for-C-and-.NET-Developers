// Chapter 4 — Section 4.3.2
// Fan-out pattern: invoke FlightsServer and HotelsServer in parallel using Task.WhenAll.
// Total latency = max(flightLatency, hotelLatency) rather than their sum.

using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Convert each ValueTask<CallToolResult> to Task<CallToolResult> so Task.WhenAll can await both.
// Both tasks are created (and therefore started) before either is awaited.
var flightTask = flightsMcpClient.CallToolAsync(
    "SearchFlightsTool",
    new Dictionary<string, object?>
    {
        ["origin"]      = "LHR",
        ["destination"] = "JFK",
        ["date"]        = "2026-06-15"
    },
    cancellationToken: ct).AsTask();

var hotelTask = hotelsMcpClient.CallToolAsync(
    "SearchHotelsTool",
    new Dictionary<string, object?>
    {
        ["city"]      = "New York",
        ["checkIn"]   = "2026-06-15",
        ["checkOut"]  = "2026-06-22"
    },
    cancellationToken: ct).AsTask();

// Both tool calls are in-flight concurrently from here.
// Task.WhenAll suspends until the slower of the two responses arrives.
await Task.WhenAll(flightTask, hotelTask);

// After WhenAll, both tasks are completed; .Result is a zero-cost synchronous extraction.
var travelOptions = CombineResults(flightTask.Result, hotelTask.Result);
