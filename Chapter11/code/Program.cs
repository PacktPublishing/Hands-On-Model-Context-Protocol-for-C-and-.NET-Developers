// Chapter 11 -- Testing and evaluating MCP agents.
//
// Runs the ch11_*.cs adaptations from Demos.cs:
//   1. Golden-test harness: an in-process stub server records tool calls made
//      by an "agent" and ToolCallEvaluator compares them to expected sequences.
//   2. Fault-injection server: every tool call throws, and the agent's
//      graceful-degradation path is classified with BehaviorClassifier.
//   3. Prompt regression harness: cosine similarity across baseline outputs
//      passes for a small edit and fails for an off-topic change.

using TravelBooking.Chapter11;

Console.WriteLine("Chapter 11 -- Testing and evaluating MCP agents");
Console.WriteLine(new string('=', 78));

Console.WriteLine();
Console.WriteLine("[1] Golden-test harness + evaluator -- ch11_1/2/3/4");

var recorder = new ToolCallRecorder();
var server = new InProcessTestServer(recorder, new()
{
	["search_flights"] = args => "[{\"flight_id\":\"BA010\"}]",
	["reserve_flight"] = args => "RES-42",
	["book_flight"]    = args => "PNR-ABCD12",
});

async Task<string[]> RunAgentAsync(string scenario)
{
	recorder.Clear();
	if (scenario == "book")
	{
		await server.CallToolAsync("search_flights", new Dictionary<string, string> { ["o"] = "LHR" });
		await server.CallToolAsync("reserve_flight", new Dictionary<string, string> { ["fid"] = "BA010" });
		await server.CallToolAsync("book_flight",    new Dictionary<string, string> { ["rid"] = "RES-42" });
	}
	else // "search-only"
	{
		await server.CallToolAsync("search_flights", new Dictionary<string, string> { ["o"] = "LHR" });
	}
	return recorder.Sequence().ToArray();
}

var golden = new[]
{
	new GoldenTestCase("full-booking",  new[] { "search_flights", "reserve_flight", "book_flight" }),
	new GoldenTestCase("search-only",   new[] { "search_flights" }),
	new GoldenTestCase("broken-case",   new[] { "search_flights", "book_flight" }),   // will fail
};
var actual = new[]
{
	await RunAgentAsync("book"),
	await RunAgentAsync("search-only"),
	await RunAgentAsync("search-only"),
};

var report = new ToolCallEvaluator().Evaluate(golden, actual);
Console.WriteLine($"  total={report.Total} passed={report.Passed} failed={report.Failed}");
foreach (var f in report.Failures)
	Console.WriteLine($"    FAIL {f.CaseName}: expected [{string.Join(",", f.Expected)}] actual [{string.Join(",", f.Actual)}]");

Console.WriteLine();
Console.WriteLine("[2] Fault-injection server + behavior classifier -- ch11_5/6");

var faulted = new FaultInjectionServer(new[] { "search_flights" },
									   new FaultSpec("simulated 503 from airline API"));
string agentOutput;
bool escalate = false;
try
{
	await faulted.CallToolAsync("search_flights", new Dictionary<string, string>());
	agentOutput = "flight booked";
}
catch (Exception ex)
{
	agentOutput = $"Sorry, I could not find any flights right now ({ex.Message}). Escalating to a human agent.";
	escalate = true;
}
var behavior = BehaviorClassifier.Classify(agentOutput, escalate);
Console.WriteLine($"  agent output   = {agentOutput}");
Console.WriteLine($"  classified as  = {behavior}");

Console.WriteLine();
Console.WriteLine("[3] Prompt regression harness -- ch11_7");

var harness = new PromptRegressionHarness();
var baseline = "I found flight BA010 from LHR to AMS for 389 USD. Would you like to book it?";
var minor    = "I found flight BA010 from LHR to AMS for 389 USD. Do you want to book it?";
var drift    = "The weather in Amsterdam is currently sunny and warm.";

var regress = harness.Compare(new[]
{
	(new RegressionCase("minor-edit", baseline, 0.85), minor),
	(new RegressionCase("off-topic",  baseline, 0.85), drift),
});
Console.WriteLine($"  similarity minor-edit = {harness.Similarity(baseline, minor):F2}");
Console.WriteLine($"  similarity off-topic  = {harness.Similarity(baseline, drift):F2}");
Console.WriteLine($"  regression report     = total={regress.Total} passed={regress.Passed} failed={regress.Failed}");
foreach (var f in regress.Failures)
	Console.WriteLine($"    FAIL {f.CaseName} (sim {f.Similarity:F2})");

Console.WriteLine();
Console.WriteLine("Chapter 11 demo complete.");
