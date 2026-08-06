// Chapter 12 -- Observability and Scale: Metrics, Tracing, Costs, and Sharding.
//
// Runs the ch12_*.cs adaptations from Demos.cs:
//   1. TokenUsageTracker aggregates prompt/completion/cached tokens (ch12_6).
//   2. BudgetCapEnforcer blocks a workflow once its token cap is exceeded (ch12_7).
//   3. LlmResponseKeyBuilder shows normalised prompts sharing a cache key and
//      scope-based TTLs (ch12_8).
//   4. ConsistentHashRouter distributes keys across a hash ring and stays
//      stable when a server is removed (ch12_9).
//   5. LoadShedder classifies requests by priority and sheds low-priority
//      traffic under load while never shedding critical calls (ch12_10).

using TravelBooking.Chapter12;

Console.WriteLine("Chapter 12 -- Observability and Scale: Metrics, Tracing, Costs, and Sharding");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] TokenUsageTracker -- ch12_6");
using var tracker = new TokenUsageTracker();
tracker.Record("tenant-A", prompt: 120, completion: 80);
tracker.Record("tenant-A", prompt:  60, completion: 40, cached: 30);
tracker.Record("tenant-B", prompt: 200, completion: 90);
Console.WriteLine($"  prompt total     = {tracker.PromptTotal}");
Console.WriteLine($"  completion total = {tracker.CompletionTotal}");
Console.WriteLine($"  cached total     = {tracker.CachedTotal}");

Console.WriteLine();
Console.WriteLine("[2] BudgetCapEnforcer -- ch12_7");
var budget = new BudgetCapEnforcer { WorkflowCapTokens = 1_000, TenantPeriodCapTokens = 10_000 };
budget.Enforce("wf-1", "tenant-A", 400);
budget.Enforce("wf-1", "tenant-A", 500);
try
{
budget.Enforce("wf-1", "tenant-A", 200);
}
catch (BudgetExceededException ex)
{
Console.WriteLine($"  blocked: {ex.Message}");
}
budget.CompleteWorkflow("wf-1");
Console.WriteLine("  workflow counter cleared after completion.");

Console.WriteLine();
Console.WriteLine("[3] LlmResponseKeyBuilder -- ch12_8");
var keys = new LlmResponseKeyBuilder();
var a = keys.BuildCacheKey("Which  tools\tare\navailable?", "tool-schema");
var b = keys.BuildCacheKey("Which tools are available?",    "tool-schema");
Console.WriteLine($"  normalised prompts share a key = {a == b}");
Console.WriteLine($"  tool-schema TTL = {keys.TtlFor("tool-schema")}");
Console.WriteLine($"  price-quote TTL = {keys.TtlFor("price-quote")}");

Console.WriteLine();
Console.WriteLine("[4] ConsistentHashRouter -- ch12_9");
var router = new ConsistentHashRouter();
router.AddServer("flights-a");
router.AddServer("flights-b");
router.AddServer("flights-c");
Console.WriteLine($"  servers registered: {router.ServerCount}");
var distribution = new Dictionary<string, int>();
foreach (var i in Enumerable.Range(0, 1000))
{
var server = router.Route($"booking-{i}");
distribution[server] = distribution.GetValueOrDefault(server) + 1;
}
foreach (var kv in distribution.OrderBy(k => k.Key))
Console.WriteLine($"    {kv.Key,-12} {kv.Value} keys");
router.RemoveServer("flights-b");
Console.WriteLine($"  servers after remove: {router.ServerCount}");
Console.WriteLine($"  route(booking-42) -> {router.Route("booking-42")}");

Console.WriteLine();
Console.WriteLine("[5] LoadShedder -- ch12_10");
var shedder = new LoadShedder();
foreach (var path in new[] { "/mcp/tools/process_payment", "/mcp/tools/book_hotel", "/mcp/tools/search_flights", "/healthz" })
{
var priority = LoadShedder.Classify(path);
var shedAt250 = shedder.ShouldShed(priority, activeRequests: 250);
Console.WriteLine($"    {path,-32} priority={priority,-10} shed@250={shedAt250}");
}

Console.WriteLine();
Console.WriteLine("Chapter 12 demo complete.");
