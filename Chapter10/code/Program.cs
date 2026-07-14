// Chapter 10 -- Multi-agent coordination with MCP.
//
// Runs the ch10_*.cs adaptations from Demos.cs:
//   1. AgentCoordinator successfully routes a request through the specialist
//      agents (flight -> hotel -> budget) under budget.
//   2. Same coordinator escalates when the combined cost exceeds the cap.
//   3. ConflictResolver picks the cheaper of two competing proposals.

using TravelBooking.Chapter10;

Console.WriteLine("Chapter 10 -- Multi-agent coordination with MCP");
Console.WriteLine(new string('=', 78));

var flight       = new FlightAgent();
var hotel        = new HotelAgent();
var escalations  = new EscalationHandler();

Console.WriteLine();
Console.WriteLine("[1] AgentCoordinator (under budget) -- ch10_2/3/5/6");
var underBudget  = new AgentCoordinator(flight, hotel, new BudgetCheckerAgent(capUsd: 1200m), escalations);
var ctx1         = new HandoffContext("sess-001");
var req1         = new AgentRequest("sess-001", "book-trip",
					new Dictionary<string, string> { ["origin"] = "LHR", ["destination"] = "AMS", ["nights"] = "3" });
var result1      = await underBudget.HandleAsync(req1, ctx1);
foreach (var r in result1.Responses)
	Console.WriteLine($"  {r.AgentName,-14} success={r.Success} summary={r.Summary}");
Console.WriteLine($"  coordinator success = {result1.Success}");
Console.WriteLine($"  handoff trail       = {string.Join(" -> ", ctx1.Trail)}");

Console.WriteLine();
Console.WriteLine("[2] AgentCoordinator (over budget -> escalation) -- ch10_3/8");
var overBudget   = new AgentCoordinator(flight, hotel, new BudgetCheckerAgent(capUsd: 500m), escalations);
var ctx2         = new HandoffContext("sess-002");
var req2         = new AgentRequest("sess-002", "book-trip",
					new Dictionary<string, string> { ["origin"] = "LHR", ["destination"] = "AMS", ["nights"] = "5" });
var result2      = await overBudget.HandleAsync(req2, ctx2);
foreach (var r in result2.Responses)
	Console.WriteLine($"  {r.AgentName,-14} success={r.Success} escalate={r.RequiresEscalation} summary={r.Summary}");
Console.WriteLine($"  coordinator success = {result2.Success}");
if (result2.Escalation is { } e)
	Console.WriteLine($"  escalation queued   = [{e.At:HH:mm:ssZ}] {e.FromAgent}: {e.Reason}");
Console.WriteLine($"  escalation queue    = {escalations.Count}");

Console.WriteLine();
Console.WriteLine("[3] ConflictResolver picks the cheaper proposal -- ch10_7");
var resolver = new ConflictResolver();
var pick = resolver.Resolve(new[]
{
	new Proposal("flight-agent-A", "AF001 direct",  512m),
	new Proposal("flight-agent-B", "BA010 direct",  389m),
	new Proposal("flight-agent-C", "KL022 1-stop",  445m),
});
Console.WriteLine($"  winner              = {pick.AgentName} '{pick.Description}' @ {pick.PriceUsd:N0} USD");

Console.WriteLine();
Console.WriteLine("Chapter 10 demo complete.");
