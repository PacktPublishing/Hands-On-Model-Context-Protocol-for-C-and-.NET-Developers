// Chapter 7 -- MCP Clients without LLMs: Streams, State, and Resilience.
using TravelBooking.Chapter07;

Console.WriteLine("Chapter 7 -- MCP Clients without LLMs: Streams, State, and Resilience");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] SessionStateCache (ch07_6)");
var now = DateTimeOffset.UtcNow;
var clock = new Func<DateTimeOffset>(() => now);
var cache = new SessionStateCache(TimeSpan.FromSeconds(5), clock);
cache.Set("session-A", "origin", "JFK");
cache.Set("session-A", "destination", "LAX");
cache.Set("session-B", "origin", "SEA");
Console.WriteLine($"  count after writes:                {cache.Count}");
cache.TryGet<string>("session-A", "origin", out var jfk);
Console.WriteLine($"  session-A.origin =>                {jfk}");
now = now.AddSeconds(4);
cache.TryGet<string>("session-A", "origin", out var jfk2);
Console.WriteLine($"  session-A.origin after +4s read => {jfk2}");
now = now.AddSeconds(10);
var purged = cache.PurgeExpired();
Console.WriteLine($"  purged expired entries:            {purged}");
Console.WriteLine($"  count after purge:                 {cache.Count}");

Console.WriteLine();
Console.WriteLine("[2] ResiliencePipeline (ch07_7)");
var pipeline = new ResiliencePipeline(
	maxAttempts: 4, attemptTimeout: TimeSpan.FromSeconds(1),
	breakAfterFailures: 6, breakDuration: TimeSpan.FromSeconds(10));
var flaky = new FlakyFlightService(failuresBeforeSuccess: 2);
var sw = System.Diagnostics.Stopwatch.StartNew();
var result = await pipeline.ExecuteAsync(ct => flaky.SearchAsync("JFK", "LAX", ct));
sw.Stop();
Console.WriteLine($"  upstream calls observed:           {flaky.Calls}");
Console.WriteLine($"  pipeline returned:                 {result}");
Console.WriteLine($"  elapsed:                           {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"  circuit state:                     {pipeline.State}");

Console.WriteLine();
Console.WriteLine("[3] Circuit-breaker opens on sustained failure");
var alwaysFails = new FlakyFlightService(failuresBeforeSuccess: int.MaxValue);
var sensitive = new ResiliencePipeline(
	maxAttempts: 6, attemptTimeout: TimeSpan.FromMilliseconds(500),
	breakAfterFailures: 3, breakDuration: TimeSpan.FromSeconds(30));
try { await sensitive.ExecuteAsync(ct => alwaysFails.SearchAsync("JFK", "LAX", ct)); }
catch (InvalidOperationException ex) { Console.WriteLine($"  caught:                            {ex.Message}"); }
Console.WriteLine($"  upstream calls before break:       {alwaysFails.Calls}");
Console.WriteLine($"  circuit state:                     {sensitive.State}");
Console.WriteLine();
Console.WriteLine("Chapter 7 demo complete.");
