// Chapter 11 -- runnable adaptations of the testing / evaluation snippets.
//
// The verbatim ch11_*.cs files depend on an in-process MCP server, xUnit,
// and the ModelContextProtocol client APIs. The types below distil the same
// ideas (tool-call recorder, golden test harness, evaluator, fault injection,
// behavior classifier, prompt regression) into self-contained code that
// Program.cs exercises without spinning up a real MCP transport.

using System.Collections.Concurrent;

namespace TravelBooking.Chapter11;

// ---------------------------------------------------------------------------
// ch11_2 -- Tool call recorder
// ---------------------------------------------------------------------------
public sealed record ToolCallRecord(string ToolName,
									IReadOnlyDictionary<string, string> Arguments,
									DateTimeOffset At);

public sealed class ToolCallRecorder
{
	private readonly ConcurrentQueue<ToolCallRecord> _calls = new();

	public void Record(string tool, IReadOnlyDictionary<string, string> args)
		=> _calls.Enqueue(new ToolCallRecord(tool, args, DateTimeOffset.UtcNow));

	public IReadOnlyList<ToolCallRecord> Snapshot() => _calls.ToArray();
	public IReadOnlyList<string> Sequence() => _calls.Select(c => c.ToolName).ToArray();
	public void Clear() { while (_calls.TryDequeue(out _)) { } }
}

// ---------------------------------------------------------------------------
// ch11_1 -- In-process stub server (returns canned tool results)
// ---------------------------------------------------------------------------
public interface IStubMcpServer
{
	IReadOnlyList<string> ToolNames { get; }
	Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, string> args,
							   CancellationToken ct = default);
}

public sealed class InProcessTestServer : IStubMcpServer
{
	private readonly Dictionary<string, Func<IReadOnlyDictionary<string, string>, string>> _tools;
	private readonly ToolCallRecorder _recorder;

	public InProcessTestServer(ToolCallRecorder recorder,
							   Dictionary<string, Func<IReadOnlyDictionary<string, string>, string>> tools)
	{
		_recorder = recorder;
		_tools = tools;
	}

	public IReadOnlyList<string> ToolNames => _tools.Keys.ToArray();

	public Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, string> args, CancellationToken ct = default)
	{
		_recorder.Record(name, args);
		if (!_tools.TryGetValue(name, out var handler))
			throw new KeyNotFoundException($"Tool '{name}' is not registered.");
		return Task.FromResult(handler(args));
	}
}

// ---------------------------------------------------------------------------
// ch11_3 -- Golden test harness + ch11_4 -- Evaluator
// ---------------------------------------------------------------------------
public sealed record GoldenTestCase(string Name, string[] ExpectedToolSequence);

public sealed record EvaluationFailure(string CaseName, string[] Expected, string[] Actual);

public sealed record EvaluationReport(int Total, int Passed, int Failed,
									  IReadOnlyList<EvaluationFailure> Failures);

public sealed class ToolCallEvaluator
{
	public EvaluationReport Evaluate(IReadOnlyList<GoldenTestCase> golden,
									 IReadOnlyList<string[]> actual)
	{
		if (golden.Count != actual.Count)
			throw new ArgumentException("golden and actual counts must match.");

		var failures = new List<EvaluationFailure>();
		for (var i = 0; i < golden.Count; i++)
		{
			if (!golden[i].ExpectedToolSequence.SequenceEqual(actual[i]))
				failures.Add(new EvaluationFailure(golden[i].Name,
												   golden[i].ExpectedToolSequence,
												   actual[i]));
		}
		return new EvaluationReport(golden.Count, golden.Count - failures.Count,
									failures.Count, failures);
	}
}

// ---------------------------------------------------------------------------
// ch11_5 -- Fault injection server (every tool returns the configured fault)
// ---------------------------------------------------------------------------
public sealed record FaultSpec(string ErrorMessage, bool IsPermanent = true);

public sealed class FaultInjectionServer : IStubMcpServer
{
	private readonly FaultSpec _fault;
	private readonly IReadOnlyList<string> _tools;

	public FaultInjectionServer(IEnumerable<string> tools, FaultSpec fault)
	{
		_tools = tools.ToArray();
		_fault = fault;
	}

	public IReadOnlyList<string> ToolNames => _tools;

	public Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, string> args, CancellationToken ct = default)
		=> Task.FromException<string>(new InvalidOperationException(_fault.ErrorMessage));
}

// ---------------------------------------------------------------------------
// ch11_6 -- Behavior classifier (categorises agent output)
// ---------------------------------------------------------------------------
public enum AgentBehavior { CompletedBooking, Clarification, Escalation, NoResults }

public static class BehaviorClassifier
{
	private static readonly string[] Clarifiers =
		{ "which destination", "please specify", "clarify", "more details" };
	private static readonly string[] NoResults =
		{ "no flights", "no available", "unavailable", "could not find" };
	private static readonly string[] Escalators =
		{ "human agent", "escalate", "manager", "handoff" };

	public static AgentBehavior Classify(string output, bool requiresEscalation)
	{
		if (requiresEscalation) return AgentBehavior.Escalation;
		var t = output.ToLowerInvariant();
		if (Escalators.Any(t.Contains)) return AgentBehavior.Escalation;
		if (NoResults.Any(t.Contains))  return AgentBehavior.NoResults;
		if (Clarifiers.Any(t.Contains)) return AgentBehavior.Clarification;
		return AgentBehavior.CompletedBooking;
	}
}

// ---------------------------------------------------------------------------
// ch11_7 -- Prompt regression harness (cosine similarity over word frequency)
// ---------------------------------------------------------------------------
public sealed record RegressionCase(string Name, string BaselineOutput,
									double MinSimilarity = 0.85);

public sealed record RegressionFailure(string CaseName, double Similarity);

public sealed record RegressionReport(int Total, int Passed, int Failed,
									  IReadOnlyList<RegressionFailure> Failures);

public sealed class PromptRegressionHarness
{
	public double Similarity(string a, string b)
	{
		var va = WordFrequency(a);
		var vb = WordFrequency(b);
		var keys = new HashSet<string>(va.Keys); keys.UnionWith(vb.Keys);

		double dot = 0, na = 0, nb = 0;
		foreach (var k in keys)
		{
			var xa = va.GetValueOrDefault(k);
			var xb = vb.GetValueOrDefault(k);
			dot += xa * xb; na += xa * xa; nb += xb * xb;
		}
		return (na == 0 || nb == 0) ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
	}

	public RegressionReport Compare(IEnumerable<(RegressionCase Case, string Actual)> pairs)
	{
		var list = pairs.ToArray();
		var failures = new List<RegressionFailure>();
		foreach (var (c, actual) in list)
		{
			var sim = Similarity(c.BaselineOutput, actual);
			if (sim < c.MinSimilarity) failures.Add(new RegressionFailure(c.Name, sim));
		}
		return new RegressionReport(list.Length, list.Length - failures.Count,
									failures.Count, failures);
	}

	private static Dictionary<string, int> WordFrequency(string text)
	{
		var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var raw in text.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';' },
										StringSplitOptions.RemoveEmptyEntries))
		{
			var w = raw.ToLowerInvariant();
			dict[w] = dict.GetValueOrDefault(w) + 1;
		}
		return dict;
	}
}
