// Chapter 9 — Section 9.2.1
// Bounded channel-backed job queue for long-running MCP workflows.
// BoundedChannelFullMode.Wait applies backpressure: WriteAsync suspends the caller
// when the channel is full rather than dropping jobs silently.
// Register as a singleton in the DI container so all components share the same queue.

using System.Threading.Channels;

namespace TravelBooking.Blazor.Services;

public sealed record McpJob(
    string Id,
    string UserMessage,
    DateTimeOffset SubmittedAt);

public sealed class McpJobQueue
{
    private readonly Channel<McpJob> _channel;

    public McpJobQueue(int capacity = 50)
    {
        _channel = Channel.CreateBounded<McpJob>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public ValueTask EnqueueAsync(
        McpJob job,
        CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<McpJob> DequeueAllAsync(
        CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);

    // Convenience factory for new jobs.
    public static McpJob CreateJob(string userMessage) =>
        new(Guid.NewGuid().ToString("N")[..12], userMessage, DateTimeOffset.UtcNow);
}
