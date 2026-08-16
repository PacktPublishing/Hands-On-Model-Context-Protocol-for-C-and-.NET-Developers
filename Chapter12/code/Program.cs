// Chapter 12 -- Observability and Scale: Metrics, Tracing, Costs, and Sharding.
using TravelBooking.Chapter12;

Console.WriteLine("Chapter 12 -- Observability and Scale: Metrics, Tracing, Costs, and Sharding");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] TokenUsageTracker (ch12_6)");
using var tracker = new TokenUsageTracker();
tracker.Record("tenant-A", prompt: 120, completion: 80);
tracker.Record("tenant-A", prompt:  60, completion: 40, cached: 30);
tracker.Record("tenant-B", prompt: 200, completion: 90);
Console.WriteLine($"  prompt total     = {tracker.PromptTotal}");
Console.WriteLine($"  completion total = {tracker.CompletionTotal}");
Console.WriteLine($"  cached total     = {tracker.CachedTotal}");

Console.WriteLine();
Console.WriteLine("[2] BudgetCapEnforcer (ch12_7)");
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
Console.WriteLine("[3] ConsistentHashRouter (ch12_9)");
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
Console.WriteLine("Chapter 12 demo complete.");
