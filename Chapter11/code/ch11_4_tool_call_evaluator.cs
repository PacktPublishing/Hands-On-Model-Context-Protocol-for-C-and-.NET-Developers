// Chapter 11 — Section 11.3
// ToolCallEvaluator and GroundingChecker: batch accuracy evaluation and
// hallucination detection for the agent's final output.

using ModelContextProtocol.Protocol;
using System.Text.RegularExpressions;

namespace TravelBooking.Testing;

public sealed record EvaluationFailure(
    string CaseName,
    string[] ExpectedSequence,
    string[] ActualSequence);

public sealed record EvaluationReport(
    int TotalCases,
    int Passed,
    int Failed,
    IReadOnlyList<EvaluationFailure> Failures);

/// <summary>
/// Compares recorded tool-call sequences against golden expectations for a batch.
/// </summary>
public sealed class ToolCallEvaluator
{
    public EvaluationReport Evaluate(
        IReadOnlyList<GoldenTestCase> golden,
        IReadOnlyList<IReadOnlyList<ToolCallRecord>> actual)
    {
        if (golden.Count != actual.Count)
            throw new ArgumentException(
                "Golden case count must match actual run count.");

        var failures = new List<EvaluationFailure>();
        for (var i = 0; i < golden.Count; i++)
        {
            var expected = golden[i].ExpectedToolSequence;
            var recorded = actual[i].Select(r => r.ToolName).ToArray();
            if (!expected.SequenceEqual(recorded))
                failures.Add(new(golden[i].Name, expected, recorded));
        }

        return new(
            golden.Count,
            golden.Count - failures.Count,
            failures.Count,
            failures);
    }
}

public sealed record GroundingResult(
    bool IsGrounded,
    IReadOnlyList<string> HallucinatedTerms);

/// <summary>
/// Checks whether the agent's output text references only flight IDs and booking
/// references that appeared in tool responses during the same run.
/// </summary>
public static class GroundingChecker
{
    // Matches patterns like SU123, BA456, LH789 (airline code + digits).
    private static readonly Regex FlightIdPattern =
        new(@"\b[A-Z]{2}\d{2,4}\b", RegexOptions.Compiled);

    // Matches booking references like REF-20250801-001.
    private static readonly Regex BookingRefPattern =
        new(@"\bREF-[\w-]+\b", RegexOptions.Compiled);

    public static GroundingResult Check(
        string agentOutput,
        IReadOnlyList<CallToolResult> toolResults)
    {
        var groundedTerms = toolResults
            .SelectMany(r => r.Content.OfType<TextContentBlock>())
            .SelectMany(b => ExtractIdentifiers(b.Text))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mentionedIds = ExtractIdentifiers(agentOutput);
        var hallucinated = mentionedIds
            .Where(id => !groundedTerms.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new(!hallucinated.Any(), hallucinated);
    }

    private static IEnumerable<string> ExtractIdentifiers(string text)
    {
        foreach (Match m in FlightIdPattern.Matches(text)) yield return m.Value;
        foreach (Match m in BookingRefPattern.Matches(text)) yield return m.Value;
    }
}
