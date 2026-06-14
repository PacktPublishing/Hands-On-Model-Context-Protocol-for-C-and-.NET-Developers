// Chapter 10 -- Security and Governance: AuthZ, Secrets, Azure API Management.
using TravelBooking.Chapter10;

Console.WriteLine("Chapter 10 -- Security and Governance: AuthZ, Secrets, Azure API Management");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] CorrelationIdMiddleware (ch10_7)");
var middleware = new CorrelationIdMiddlewareDemo(new CorrelationIdGenerator());
var headers1 = new Dictionary<string, string>();
var r1 = await middleware.InvokeAsync(headers1, (id, _) => Task.FromResult($"handled with id={id}"));
Console.WriteLine($"  no incoming id     -> generated {r1.Correlation}");
Console.WriteLine($"                        downstream result: {r1.Result}");

var headers2 = new Dictionary<string, string> { ["X-Correlation-Id"] = "client-supplied-42" };
var r2 = await middleware.InvokeAsync(headers2, (id, _) => Task.FromResult($"handled with id={id}"));
Console.WriteLine($"  incoming id        -> preserved {r2.Correlation}");

Console.WriteLine();
Console.WriteLine("[2] TenantIsolationGuard (ch10_3)");
var guard = new TenantIsolationGuard();
guard.Register(new TenantResource("booking-001", "tenant-A"));
guard.Register(new TenantResource("booking-002", "tenant-B"));
var allowed = guard.Read("tenant-A", "booking-001");
Console.WriteLine($"  same-tenant read   -> ok ({allowed.ResourceId})");
try
{
	guard.Read("tenant-A", "booking-002");
}
catch (CrossTenantAccessException ex)
{
	Console.WriteLine($"  cross-tenant read  -> blocked: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Chapter 10 demo complete.");
