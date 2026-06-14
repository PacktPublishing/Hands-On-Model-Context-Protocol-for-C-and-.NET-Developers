// Chapter 8 — Section 8.3.2
// IGuardrail implementation that detects prompt injection patterns in tool arguments.
// Scans all string argument values for known override phrases before the tool executes.
// Complement with Azure AI Content Safety for production-grade classification.

using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Orchestration.Guardrails;

public static class PromptInjectionDetector
{
    private static readonly string[] Patterns =
    [
        "ignore all previous",
        "ignore your instructions",
        "disregard the system prompt",
        "you are now",
        "new instructions:"
    ];

    public static bool Contains(string input) =>
        Patterns.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
}

public sealed class PromptInjectionGuardrail : IGuardrail
{
    public Task<GuardrailResult> CheckAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        foreach (var value in arguments.Values)
        {
            if (value is string text && PromptInjectionDetector.Contains(text))
                return Task.FromResult(GuardrailResult.Reject(
                    "Input contains a prompt injection pattern."));
        }

        return Task.FromResult(GuardrailResult.Allow());
    }
}
