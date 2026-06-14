// Chapter 9 — Section 9.1.4
// Typed MCP error classification for Blazor components.
// Converts raw exceptions to McpUiError records that components render appropriately:
// transient transport errors → retry button; guardrail rejections → explanation; cancellation → silence.

using ModelContextProtocol;
using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Blazor.Services;

public enum Severity { Info, Warning, Error }

public sealed record McpUiError(
    Severity Severity,
    string Message,
    bool CanRetry);

public sealed class McpErrorHandler(IConnectivityService connectivity)
{
    public McpUiError? Classify(Exception ex) => ex switch
    {
        OperationCanceledException => null,

        HttpRequestException =>
            new McpUiError(
                Severity.Error,
                connectivity.IsOnline
                    ? "The booking service is temporarily unavailable."
                    : "You appear to be offline. Cached results are shown.",
                CanRetry: true),

        McpException mcpEx =>
            new McpUiError(Severity.Error, mcpEx.Message, CanRetry: true),

        GuardrailRejectedException guardEx =>
            new McpUiError(Severity.Warning, guardEx.RejectionReason, CanRetry: false),

        _ => new McpUiError(
            Severity.Error,
            "An unexpected error occurred. Please try again.",
            CanRetry: false)
    };
}

// IConnectivityService is defined in Section 9.3.2 (ch09_10_connectivity_service.cs).
// This interface declaration is here for compilation context.
public interface IConnectivityService
{
    bool IsOnline { get; }
    event Action<bool>? OnConnectivityChanged;
}
