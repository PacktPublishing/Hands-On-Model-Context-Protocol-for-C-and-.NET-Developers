// Chapter 11 — Section 11.2
// GoldenTestCase and GoldenTestHarness: define expected tool-call sequences
// and run them against an agent, collecting pass/fail results.

using TravelBooking.MultiAgent;

namespace TravelBooking.Testing;

/// <summary>
/// A golden test case: a user request paired with the expected ordered tool-call
/// sequence. AlternativeSequences allows for multiple correct orderings.
/// </summary>
public sealed record GoldenTestCase(
    string Name,
    string SessionId,
    string UserRequest,
    string[] ExpectedToolSequence,
    string[][]? AlternativeSequences = null);

public sealed record GoldenTestResult(
    string CaseName,
    bool Passed,
    string[] ExpectedSequence,
    string[] ActualSequence,
    string? FailureReason = null);

/// <summary>
/// Runs a batch of golden test cases against an agent under test.
/// Each case gets its own recorder and in-process stub server.
/// </summary>
public sealed class GoldenTestHarness(
    Func<McpClient, ISpecialistAgent> agentFactory,
    IReadOnlyList<McpServerTool> stubTools)
{
    public async Task<IReadOnlyList<GoldenTestResult>> RunAsync(
        IReadOnlyList<GoldenTestCase> cases,
        CancellationToken ct = default)
    {
        var results = new List<GoldenTestResult>();
        foreach (var golden in cases)
        {
            await using var server =
                await InProcessTestServer.CreateAsync(stubTools, ct);
            var recorder = new ToolCallRecorder(server.Client);
            var agent = agentFactory(server.Client);

            AgentResult agentResult;
            try
            {
                agentResult = await agent.RunAsync(
                    new HandoffToken(
                        golden.SessionId,
                        agent.AgentId,
                        golden.UserRequest), ct);
            }
            catch (Exception ex)
            {
                results.Add(new(golden.Name, false,
                    golden.ExpectedToolSequence, [],
                    $"Agent threw exception: {ex.Message}"));
                continue;
            }

            var actual = recorder.ToolNames().ToArray();
            var passed = MatchesAnySequence(
                actual,
                golden.ExpectedToolSequence,
                golden.AlternativeSequences);

            results.Add(new(
                golden.Name, passed,
                golden.ExpectedToolSequence, actual,
                passed ? null :
                    $"Sequence mismatch. " +
                    $"Expected [{string.Join(",", golden.ExpectedToolSequence)}], " +
                    $"got [{string.Join(",", actual)}]"));
        }
        return results;
    }

    private static bool MatchesAnySequence(
        string[] actual,
        string[] primary,
        string[][]? alternatives)
    {
        if (actual.SequenceEqual(primary)) return true;
        if (alternatives is null) return false;
        return alternatives.Any(alt => actual.SequenceEqual(alt));
    }
}
