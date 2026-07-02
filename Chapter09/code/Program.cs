// Chapter 9 -- Building agentic workflows with MCP and .NET.
//
// Runs five real demos of the agentic-workflow adaptations from Demos.cs:
//   1. Happy-path workflow through IdleState -> ConfirmedState.
//   2. TransitionGuard rejects a tool that is not valid in the current state.
//   3. WorkflowBudget stops a runaway loop.
//   4. SustainedErrorGuard triggers an emergency stop after N consecutive failures.
//   5. WorkflowResumer decides the correct recovery action from persisted state.

using TravelBooking.Chapter09;

Console.WriteLine("Chapter 9 -- Building agentic workflows with MCP and .NET");
Console.WriteLine(new string('=', 78));

var store   = new WorkflowStateStore();
var guard   = new TransitionGuard();
var budget  = new WorkflowBudget(maxToolCalls: 10);
var errors  = new SustainedErrorGuard(threshold: 3);

// A tiny in-memory MCP-style tool dispatcher. Real code would call an MCP client here.
Task<object?> DispatchAsync(string tool, IReadOnlyDictionary<string, object?> args, CancellationToken ct) =>
	tool switch
	{
		"search_flights" => Task.FromResult<object?>((IReadOnlyList<FlightOption>)new[]
		{
			new FlightOption("AF001", "AF", 412m),
			new FlightOption("BA010", "BA", 389m),
			new FlightOption("KL022", "KL", 445m),
		}),
		"reserve_flight" => Task.FromResult<object?>("RES-" + Guid.NewGuid().ToString("N").Substring(0, 8)),
		"book_flight"    => Task.FromResult<object?>("PNR-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()),
		_ => Task.FromException<object?>(new InvalidOperationException($"Unknown tool '{tool}'."))
	};

var executor = new TravelExecutorAgent(store, guard, budget, errors, DispatchAsync);

Console.WriteLine();
Console.WriteLine("[1] Happy-path workflow (ch09_3 / ch09_4 / ch09_8)");
var plan = new TravelPlan("LHR", "AMS", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
						  new[] { "search", "reserve", "book" });
var run  = await executor.ExecuteAsync("wf-001", plan);
Console.WriteLine($"  trace:  {string.Join(" -> ", run.Trace)}");
Console.WriteLine($"  final:  {run.FinalState.GetType().Name}");
if (run.FinalState is ConfirmedState cs)
	Console.WriteLine($"  PNR:    {cs.BookingReference}");

Console.WriteLine();
Console.WriteLine("[2] TransitionGuard rejects an illegal tool (ch09_11)");
try
{
	guard.AssertAllowed(new SearchingState("LHR", "AMS", DateOnly.FromDateTime(DateTime.UtcNow)),
						"book_flight");
}
catch (InvalidTransitionException ex)
{
	Console.WriteLine($"  blocked: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("[3] WorkflowBudget stops a runaway loop (ch09_10)");
var tinyBudget = new WorkflowBudget(maxToolCalls: 3);
try
{
	for (var i = 1; i <= 10; i++) tinyBudget.Consume("wf-loop");
}
catch (WorkflowBudgetExceededException ex)
{
	Console.WriteLine($"  blocked: {ex.Message} (used before block: {tinyBudget.Used("wf-loop")})");
}

Console.WriteLine();
Console.WriteLine("[4] SustainedErrorGuard triggers emergency stop (ch09_12)");
var guard2 = new SustainedErrorGuard(threshold: 3);
try
{
	for (var i = 1; i <= 5; i++) guard2.RecordFailure("book_flight");
}
catch (EmergencyStopException ex)
{
	Console.WriteLine($"  blocked: {ex.Message}");
}
Console.WriteLine($"  consecutive failures: {guard2.ConsecutiveFailures("book_flight")}");

Console.WriteLine();
Console.WriteLine("[5] WorkflowResumer chooses correct recovery action (ch09_9)");
var resumer = new WorkflowResumer(store);
await store.TransitionAsync("wf-002", new AwaitingApprovalState("RES-42", DateTimeOffset.UtcNow.AddMinutes(15)));
await store.TransitionAsync("wf-003", new FailedState("BookingStep", "network glitch"));
await store.TransitionAsync("wf-004", new ConfirmedState("PNR-DONE"));

foreach (var id in new[] { "wf-002", "wf-003", "wf-004" })
{
	var outcome = await resumer.ResumeAsync(id);
	Console.WriteLine($"  {id,-8} -> {outcome.Action,-45} state={outcome.State.GetType().Name}");
}

Console.WriteLine();
Console.WriteLine("Chapter 9 demo complete.");
