// Chapter 3 — Section 3.2.5
// First resource: FlightResources class with a URI-templated GetFlightStatus stub.

using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerResourceType]
public static class FlightResources
{
    [McpServerResource(UriTemplate = "flight://status/{flightId}")]
    [Description("Returns the current status of a flight by its IATA flight identifier.")]
    public static string GetFlightStatus(
        [Description("IATA flight identifier, e.g. BA0293.")] string flightId)
    {
        // Stub: real data source wired in Chapter 5.
        return $"Flight {flightId}: on time, gate B12, departure 14:35 UTC.";
    }
}
