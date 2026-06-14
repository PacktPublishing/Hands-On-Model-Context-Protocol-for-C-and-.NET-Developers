// Chapter 8 — Section 8.3.1
// Guardrail pipeline and interface for validating tool calls before execution.
// Each IGuardrail implementation can allow or reject a call; the first rejection stops the chain.
// Register guardrails cheapest-first: SchemaGuard before ApprovalGate.

namespace TravelBooking.Orchestration.Guardrails;

public interface IGuardrail
{
    Task<GuardrailResult> CheckAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}

public sealed record GuardrailResult(
    bool IsAllowed,
    string? RejectionReason = null)
{
    public static GuardrailResult Allow() => new(IsAllowed: true);
    public static GuardrailResult Reject(string reason) =>
        new(IsAllowed: false, RejectionReason: reason);
}

public sealed class GuardrailRejectedException(
    string toolName,
    string rejectionReason)
    : Exception($"Guardrail blocked '{toolName}': {rejectionReason}")
{
    public string ToolName { get; } = toolName;
    public string RejectionReason { get; } = rejectionReason;
}

public sealed class GuardrailPipeline
{
    private readonly IReadOnlyList<IGuardrail> _guards;

    public GuardrailPipeline(IReadOnlyList<IGuardrail> guards) =>
        _guards = guards;

    public GuardrailPipeline(IEnumerable<IGuardrail> guards) =>
        _guards = guards.ToList();

    public async Task AssertAllowedAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken ct = default)
    {
        foreach (var guard in _guards)
        {
            var result = await guard.CheckAsync(toolName, args, ct);
            if (!result.IsAllowed)
                throw new GuardrailRejectedException(
                    toolName, result.RejectionReason!);
        }
    }
}

// DI registration — add to Program.cs or a service collection extension:
//
//   builder.Services.AddSingleton<IGuardrail, PromptInjectionGuardrail>();
//   builder.Services.AddSingleton<IGuardrail, SchemaGuard>();
//   builder.Services.AddSingleton<IGuardrail, ApprovalGate>();
//   builder.Services.AddSingleton<GuardrailPipeline>(sp =>
//       new GuardrailPipeline(sp.GetServices<IGuardrail>().ToList()));
