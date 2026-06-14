// Chapter 9 -- UX Integration: Blazor and .NET MAUI, Background Work, and Offline.
using TravelBooking.Chapter09;

Console.WriteLine("Chapter 9 -- UX Integration: Blazor and .NET MAUI, Background Work, and Offline");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] OfflineRetryQueue (ch09_11) -- offline then reconnect");
var queue = new OfflineRetryQueue(maxAttempts: 3);
queue.Enqueue("search_flights", "{\"origin\":\"JFK\"}");
queue.Enqueue("search_flights", "{\"origin\":\"LAX\"}");
queue.Enqueue("book_flight",    "{\"flight\":\"AA100\"}");
Console.WriteLine($"  queued: {queue.Pending}");

var failOnce = new HashSet<string>();
var drain = await queue.DrainAsync(async (op, ct) =>
{
	await Task.Yield();
	if (op.ToolName == "book_flight" && failOnce.Add(op.Id)) return false;   // first attempt fails
	return true;
});
Console.WriteLine($"  drain #1: succeeded={drain.Succeeded} requeued={drain.Requeued} dropped={drain.Dropped} pending={queue.Pending}");

var drain2 = await queue.DrainAsync((op, ct) => Task.FromResult(true));
Console.WriteLine($"  drain #2: succeeded={drain2.Succeeded} requeued={drain2.Requeued} dropped={drain2.Dropped} pending={queue.Pending}");

Console.WriteLine();
Console.WriteLine("[2] CachingClient (ch09_9) -- hit vs miss with TTL");
var nowRef = DateTimeOffset.UtcNow;
var clock = new Func<DateTimeOffset>(() => nowRef);
var cache = new CachingClient<string, int>(TimeSpan.FromSeconds(2), clock);
var upstreamCalls = 0;
Task<int> Upstream(string key, CancellationToken ct) { upstreamCalls++; return Task.FromResult(key.Length); }

await cache.InvokeAsync("JFK->LAX", Upstream);   // miss
await cache.InvokeAsync("JFK->LAX", Upstream);   // hit
await cache.InvokeAsync("SEA->ORD", Upstream);   // miss
nowRef = nowRef.AddSeconds(3);
await cache.InvokeAsync("JFK->LAX", Upstream);   // miss (expired)
Console.WriteLine($"  cache hits={cache.Hits} misses={cache.Misses} upstream_calls={upstreamCalls}");

Console.WriteLine();
Console.WriteLine("Chapter 9 demo complete.");
