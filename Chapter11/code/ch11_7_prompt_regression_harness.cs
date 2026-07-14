// Chapter 11 — Section 11.5
// PromptRegressionHarness: stores baseline outputs in JSON fixtures, compares
// subsequent runs using cosine similarity over word-frequency vectors.
// Run in capture mode (UpdateBaselines = true) to record new baselines;
// run in regression mode (UpdateBaselines = false) for CI comparison.

using System.Text.Json;
using TravelBooking.MultiAgent;

namespace TravelBooking.Testing;

public sealed record RegressionCase(
    string Name,
    string SessionId,
    string Input,
    string BaselineOutput,
    double MinSimilarityThreshold = 0.85);

public sealed record RegressionFailure(
    string CaseName,
    string BaselineOutput,
    string ActualOutput,
    double ActualSimilarity);

public sealed record RegressionReport(
    int TotalCases,
    int Passed,
    int Failed,
    IReadOnlyList<RegressionFailure> Failures);

public sealed class PromptRegressionHarness(
    ISpecialistAgent agent,
    string fixtureDirectory,
    bool updateBaselines = false)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    public async Task<RegressionReport> RunAsync(
        IReadOnlyList<RegressionCase> cases,
        CancellationToken ct = default)
    {
        var failures = new List<RegressionFailure>();
        foreach (var c in cases)
        {
            var result = await agent.RunAsync(
                new HandoffToken(c.SessionId, agent.AgentId, c.Input), ct);

            var normalized = Normalize(result.Output);

            if (updateBaselines)
            {
                SaveBaseline(c.Name, normalized);
                continue;
            }

            var baseline = Normalize(c.BaselineOutput);
            var similarity = CosineSimilarity(baseline, normalized);

            if (similarity < c.MinSimilarityThreshold)
                failures.Add(new(c.Name, c.BaselineOutput,
                    result.Output, similarity));
        }

        return new(cases.Count, cases.Count - failures.Count,
            failures.Count, failures);
    }

    /// <summary>
    /// Remove dynamic tokens (booking references, dates) that would cause
    /// every run to fail because the values change per invocation.
    /// </summary>
    private static string Normalize(string text) =>
        System.Text.RegularExpressions.Regex.Replace(
            text,
            @"REF-[\w-]+|\d{4}-\d{2}-\d{2}",
            "DYNAMIC");

    private static double CosineSimilarity(string a, string b)
    {
        var va = TermFrequency(a);
        var vb = TermFrequency(b);
        var dot = va.Keys.Intersect(vb.Keys)
            .Sum(k => va[k] * vb[k]);
        var magA = Math.Sqrt(va.Values.Sum(v => v * v));
        var magB = Math.Sqrt(vb.Values.Sum(v => v * v));
        return magA == 0 || magB == 0 ? 0 : dot / (magA * magB);
    }

    private static Dictionary<string, double> TermFrequency(string text)
    {
        var words = text.ToLowerInvariant()
            .Split([' ', '\n', '\r', ',', '.', '!', '?'],
                StringSplitOptions.RemoveEmptyEntries);
        var freq = new Dictionary<string, double>();
        foreach (var w in words)
            freq[w] = freq.GetValueOrDefault(w) + 1.0;
        return freq;
    }

    private void SaveBaseline(string caseName, string output)
    {
        var path = Path.Combine(
            fixtureDirectory, $"{caseName}.baseline.json");
        File.WriteAllText(path,
            JsonSerializer.Serialize(new { caseName, output }, JsonOptions));
    }
}
