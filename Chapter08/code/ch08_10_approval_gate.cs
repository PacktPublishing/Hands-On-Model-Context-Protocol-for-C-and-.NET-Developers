// Chapter 8 — Section 8.3.6
// Approval gate that pauses the planning loop before high-stakes tool calls.
// IApprovalProvider decouples the gate from the delivery mechanism:
// console in CLI tools, dialog in Blazor (Chapter 9), auto-approve in test pipelines.

using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Orchestration.Guardrails;

public interface IApprovalProvider
{
    Task<bool> RequestApprovalAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default);
}

public sealed class ApprovalGate(IApprovalProvider approvals)
    : IGuardrail
{
    private static readonly HashSet<string> HighStakes =
        ["book_flight", "cancel_flight", "process_payment"];

    public async Task<GuardrailResult> CheckAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken ct = default)
    {
        if (!HighStakes.Contains(toolName))
            return GuardrailResult.Allow();

        var approved = await approvals.RequestApprovalAsync(toolName, args, ct);
        return approved
            ? GuardrailResult.Allow()
            : GuardrailResult.Reject("User denied the action.");
    }
}

// Console-based implementation for CLI tools and development.
public sealed class ConsoleApprovalProvider : IApprovalProvider
{
    public Task<bool> RequestApprovalAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"\nApproval required: {toolName}");
        Console.WriteLine($"Arguments: {JsonSerializer.Serialize(args, new JsonSerializerOptions { WriteIndented = true })}");
        Console.Write("Approve? [y/N] ");
        var response = Console.ReadLine() ?? string.Empty;
        return Task.FromResult(
            response.Trim().Equals("y", StringComparison.OrdinalIgnoreCase));
    }
}

// Auto-approving stub for automated test pipelines.
public sealed class AutoApprovalProvider(bool autoApprove = true)
    : IApprovalProvider
{
    public Task<bool> RequestApprovalAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
        => Task.FromResult(autoApprove);
}
