// Chapter 11 -- Cloud Deployment: Azure Functions and Container Apps with CI/CD.
using TravelBooking.Chapter11;

Console.WriteLine("Chapter 11 -- Cloud Deployment: Azure Functions and Container Apps with CI/CD");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] CapabilityLibrary (ch11_2)");
var lib = new CapabilityLibrary();
lib.Register(new Capability("flights",  new Version(1, 0), "https://api/v1"));
lib.Register(new Capability("flights",  new Version(2, 1), "https://api/v2"));
lib.Register(new Capability("hotels",   new Version(1, 0), "https://hotels/v1"));
var resolved = lib.Resolve("flights", new Version(2, 0));
Console.WriteLine($"  resolve flights >=2.0 -> {resolved?.Version} @ {resolved?.Endpoint}");
foreach (var c in lib.ListAll())
	Console.WriteLine($"    catalog: {c.Name,-8} {c.Version}  {c.Endpoint}");

Console.WriteLine();
Console.WriteLine("[2] RollbackController (ch11_6)");
var rollback = new RollbackController(new DeploymentSlot("flights-server", "1.0.0"));

var promote = await rollback.DeployAsync(new DeploymentSlot("flights-server", "1.1.0"),
										 (slot, _) => Task.FromResult(true));
Console.WriteLine($"  active after healthy deploy : {promote.Version}");

var keep = await rollback.DeployAsync(new DeploymentSlot("flights-server", "1.2.0"),
									  (slot, _) => Task.FromResult(false));
Console.WriteLine($"  active after failed deploy  : {keep.Version}");

var reverted = rollback.Rollback();
Console.WriteLine($"  active after rollback       : {reverted.Version}");
Console.WriteLine("  event log:");
foreach (var e in rollback.Events) Console.WriteLine($"    {e}");

Console.WriteLine();
Console.WriteLine("Chapter 11 demo complete.");
