// Chapter 7 — Section 7.2.1 / 7.2.2
// Progress notifications via IProgress<ProgressNotificationValue> and Channel<T>.
// Routes SDK progress callbacks through a bounded channel to apply backpressure:
// BoundedChannelFullMode.Wait blocks the callback thread when the consumer is slow.
// CallToolWithProgressAsync exposes progress as IAsyncEnumerable for clean consumption.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace TravelBooking.Client;

public static class McpClientProgressExtensions
{
    // Wraps CallToolAsync and exposes progress updates as IAsyncEnumerable<ProgressNotificationValue>.
    // The bounded channel (capacity 16) applies backpressure: the SDK callback blocks when full.
    // channel.Writer.Complete() in the continuation signals ReadAllAsync to stop after the call.
    public static async IAsyncEnumerable<ProgressNotificationValue>
        CallToolWithProgressAsync(
            this McpClient client,
            string toolName,
            IReadOnlyDictionary<string, object?> args,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<ProgressNotificationValue>(
            new BoundedChannelOptions(16)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });

        var progress = new Progress<ProgressNotificationValue>(
            update => channel.Writer.TryWrite(update));

        var callTask = client.CallToolAsync(toolName, args, progress,
                cancellationToken: cancellationToken)
            .AsTask()
            .ContinueWith(_ => channel.Writer.Complete(), TaskScheduler.Default);

        await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken))
            yield return update;

        await callTask;
    }
}

// Usage example — book_flight with live progress display.
public sealed class BookFlightProgressConsumer
{
    private readonly McpClient _client;

    public BookFlightProgressConsumer(McpClient client) => _client = client;

    public async Task BookWithProgressAsync(
        string flightId, string correlationId, string passengerName,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["flightId"] = flightId,
            ["idempotencyKey"] = correlationId,
            ["passengerName"] = passengerName
        };

        await foreach (var update in _client.CallToolWithProgressAsync(
            "book_flight", args, cancellationToken))
        {
            var display = update.Total.HasValue
                ? $"{update.Progress / update.Total.Value:P0}"
                : $"{update.Progress} steps completed";
            Console.WriteLine($"[book_flight] {display}");
        }
    }
}
