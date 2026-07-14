// Chapter 11 — Section 11.2
// ToolCallRecorder: wraps McpClient to intercept and record every CallToolAsync
// invocation. Exposes helpers for asserting tool-call sequences in golden tests.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.Testing;

/// <summary>A single recorded tool invocation.</summary>
public sealed record ToolCallRecord(
    string ToolName,
    IReadOnlyDictionary<string, object?> Args,
    DateTimeOffset CalledAt,
    bool WasError);

/// <summary>
/// Intercepts every CallToolAsync on the wrapped McpClient.
/// Create a new instance per test — the internal list is not reset between runs.
/// </summary>
public sealed class ToolCallRecorder(McpClient inner)
{
    private readonly List<ToolCallRecord> _calls = [];

    public IReadOnlyList<ToolCallRecord> Calls => _calls;

    /// <summary>Returns tool names in invocation order.</summary>
    public IEnumerable<string> ToolNames() =>
        _calls.Select(c => c.ToolName);

    /// <summary>
    /// Records the call and forwards to the underlying McpClient.
    /// Use this instead of calling inner.CallToolAsync directly.
    /// </summary>
    public async Task<CallToolResult> RecordAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? args = null,
        CancellationToken ct = default)
    {
        var result = await inner.CallToolAsync(toolName, args, ct: ct);
        _calls.Add(new(
            toolName,
            args ?? new Dictionary<string, object?>(),
            DateTimeOffset.UtcNow,
            result.IsError is true));
        return result;
    }

    /// <summary>
    /// Asserts that the recorded tool sequence matches the expected sequence
    /// and throws <see cref="SequenceAssertionException"/> on mismatch.
    /// </summary>
    public void AssertSequence(params string[] expectedTools)
    {
        var actual = ToolNames().ToArray();
        if (!actual.SequenceEqual(expectedTools))
            throw new SequenceAssertionException(expectedTools, actual);
    }

    /// <summary>Returns all argument values for a given tool, in call order.</summary>
    public IEnumerable<IReadOnlyDictionary<string, object?>> ArgsFor(string toolName) =>
        _calls.Where(c => c.ToolName == toolName).Select(c => c.Args);
}

public sealed class SequenceAssertionException(
    string[] expected, string[] actual)
    : Exception(
        $"Tool sequence mismatch.\n" +
        $"  Expected: [{string.Join(", ", expected)}]\n" +
        $"  Actual:   [{string.Join(", ", actual)}]");
