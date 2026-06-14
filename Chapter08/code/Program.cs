// Chapter 8 -- LLM-Integrated Clients: Orchestration, Tool Use, and Safety.
using TravelBooking.Chapter08;

Console.WriteLine("Chapter 8 -- LLM-Integrated Clients: Orchestration, Tool Use, and Safety");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] LoopDetector (ch08_2)");
var detector = new LoopDetector(maxRepeats: 3);
var searchArgs = new { origin = "JFK", destination = "LAX", date = "2025-12-01" };
for (var i = 1; i <= 5; i++)
{
	var isLoop = detector.RegisterCall("search_flights", searchArgs);
	Console.WriteLine($"  call #{i} loop-detected? {isLoop} (repeat count = {detector.RepeatCount("search_flights", searchArgs)})");
}
detector.RegisterCall("book_flight", new { flight = "AA100" });
Console.WriteLine($"  distinct fingerprints: {detector.DistinctCalls}");

Console.WriteLine();
Console.WriteLine("[2] AuditLogger (ch08_9)");
var t0 = new DateTimeOffset(2025, 11, 19, 12, 0, 0, TimeSpan.Zero);
var tick = 0;
var audit = new AuditLogger(() => t0.AddSeconds(tick++));
audit.Log("planner", "decide", new Dictionary<string, string> { ["plan"] = "search-then-book" });
audit.Log("tool", "search_flights", new Dictionary<string, string> { ["origin"] = "JFK", ["dest"] = "LAX" });
audit.Log("guardrail", "schema-check", new Dictionary<string, string> { ["result"] = "pass" });
audit.Log("tool", "book_flight", new Dictionary<string, string> { ["flight"] = "AA100" });
foreach (var record in audit.Snapshot())
	Console.WriteLine($"  #{record.Sequence:D3} {record.Timestamp:HH:mm:ss} {record.Actor,-10} {record.Action} {string.Join(",", record.Metadata)}");

Console.WriteLine();
Console.WriteLine("Chapter 8 demo complete.");
