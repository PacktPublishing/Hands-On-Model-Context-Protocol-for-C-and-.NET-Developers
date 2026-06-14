// Chapter 11 — Section 11.4.3
// Dapr sidecar integration for cross-cutting infrastructure concerns.
// Service invocation routes via Dapr app ID rather than a URL; the sidecar handles
// service discovery, retries, and mutual TLS automatically.
// State management abstracts the backing store: swap Redis for Cosmos DB by changing
// a Dapr component YAML — no application code changes required.
// Pub/sub enables event-driven communication between MCP servers without direct coupling.

using Dapr.Client;

namespace TravelBooking.Orchestrator.Services;

public sealed class DaprFlightService(DaprClient dapr)
{
    private const string FlightsAppId    = "flights-mcp-server";
    private const string StateStoreName  = "statestore";
    private const string PubSubName      = "pubsub";
    private const string BookingTopic    = "booking-confirmed";

    // Service invocation: no URL or port — Dapr resolves via app ID and applies mTLS.
    public async Task<FlightSearchResult> SearchFlightsAsync(
        FlightSearchRequest request, CancellationToken ct = default) =>
        await dapr.InvokeMethodAsync<FlightSearchRequest, FlightSearchResult>(
            HttpMethod.Post, FlightsAppId, "tools/call", request, cancellationToken: ct);

    // State management: the storage backend is swappable via a Dapr component definition.
    public async Task SaveBookingSessionAsync(
        string sessionId, BookingSession session, CancellationToken ct = default) =>
        await dapr.SaveStateAsync(StateStoreName, sessionId, session, cancellationToken: ct);

    public async Task<BookingSession?> GetBookingSessionAsync(
        string sessionId, CancellationToken ct = default) =>
        await dapr.GetStateAsync<BookingSession>(StateStoreName, sessionId, cancellationToken: ct);

    public async Task DeleteBookingSessionAsync(
        string sessionId, CancellationToken ct = default) =>
        await dapr.DeleteStateAsync(StateStoreName, sessionId, cancellationToken: ct);

    // Pub/sub: publishes a domain event that downstream consumers subscribe to independently.
    public async Task PublishBookingConfirmedAsync(
        BookingConfirmedEvent evt, CancellationToken ct = default) =>
        await dapr.PublishEventAsync(PubSubName, BookingTopic, evt, ct);
}

// DI registration in Program.cs:
//   builder.Services.AddDaprClient();
//   builder.Services.AddScoped<DaprFlightService>();

public record FlightSearchRequest(string Origin, string Destination, DateOnly Date);
public record FlightSearchResult(string FlightNumber, DateOnly DepartureDate, decimal Price);
public record BookingSession(string SessionId, string TenantId, DateTimeOffset CreatedAt);
public record BookingConfirmedEvent(string BookingId, string TenantId, DateTimeOffset ConfirmedAt);
