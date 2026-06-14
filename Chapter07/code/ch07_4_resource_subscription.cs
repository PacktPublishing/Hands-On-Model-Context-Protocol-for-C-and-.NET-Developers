// Chapter 7 — Section 7.2.3
// Resource subscriptions via the combined SubscribeToResourceAsync overload.
// The overload registers both the subscription request and the notification handler.
// The returned IAsyncDisposable unsubscribes and removes the handler when disposed.
// await using disposes cleanly; forgetting it leaves the subscription open indefinitely.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.Client;

public sealed class ItinerarySubscriptionManager : IAsyncDisposable
{
    private readonly McpClient _client;
    private readonly Dictionary<string, IAsyncDisposable> _subscriptions = new();

    public ItinerarySubscriptionManager(McpClient client) => _client = client;

    // Subscribe to a booking's itinerary resource and invoke the callback on every update.
    // The IAsyncDisposable is stored so the subscription can be cancelled by booking reference.
    public async Task SubscribeAsync(
        string bookingReference,
        Func<string, CancellationToken, Task> onUpdate,
        CancellationToken cancellationToken = default)
    {
        var uri = $"travel://itineraries/{bookingReference}";

        var subscription = await _client.SubscribeToResourceAsync(
            uri,
            async (notification, ct) =>
            {
                var updated = await _client.ReadResourceAsync(
                    notification.Uri, cancellationToken: ct);

                if (updated.Contents.Count > 0 &&
                    updated.Contents[0] is TextResourceContents text)
                    await onUpdate(text.Text, ct);
            },
            cancellationToken: cancellationToken);

        _subscriptions[bookingReference] = subscription;
    }

    public async Task UnsubscribeAsync(string bookingReference)
    {
        if (_subscriptions.Remove(bookingReference, out var sub))
            await sub.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sub in _subscriptions.Values)
            await sub.DisposeAsync();
        _subscriptions.Clear();
    }
}
