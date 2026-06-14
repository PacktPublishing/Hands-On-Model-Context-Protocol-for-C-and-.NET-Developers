// Chapter 9 — Section 9.5.3
// MAUI-specific IApprovalProvider that invokes the device's native authentication UI.
// ILocalAuthService is defined in the Razor Class Library (TravelBooking.UI) and implemented
// in the MAUI host project using the platform's biometric or PIN authentication API.
// allowFallback: true lets users without biometric hardware authenticate with device PIN/password.

using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Maui.Services;

// Interface defined in the shared Razor Class Library (TravelBooking.UI).
// Platform-specific implementations register in each host's DI container.
public interface ILocalAuthService
{
    Task<bool> AuthenticateAsync(
        string title,
        bool allowFallback = true,
        CancellationToken ct = default);
}

public sealed class NativeApprovalProvider(
    ILocalAuthService auth) : IApprovalProvider
{
    private static readonly HashSet<string> HighStakes =
        ["book_flight", "cancel_flight", "process_payment"];

    public async Task<bool> RequestApprovalAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (!HighStakes.Contains(toolName))
            return true;

        return await auth.AuthenticateAsync(
            title: $"Approve: {TranslateToolName(toolName)}",
            allowFallback: true,
            ct: cancellationToken);
    }

    private static string TranslateToolName(string toolName) => toolName switch
    {
        "book_flight"      => "Book flight",
        "cancel_flight"    => "Cancel flight",
        "process_payment"  => "Process payment",
        _                  => toolName
    };
}

// Blazor Server implementation — shows a confirmation dialog in the browser.
// Register BlazorApprovalProvider in TravelBooking.Web's DI container.
public sealed class BlazorApprovalProvider : IApprovalProvider
{
    // In a real implementation, inject a dialog service (e.g., MudBlazor IDialogService)
    // and await the user's confirmation through the Blazor component tree.
    public Task<bool> RequestApprovalAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        // Placeholder — replace with your Blazor dialog invocation.
        // Example: return await _dialogService.ShowConfirmationAsync(toolName, args);
        throw new NotImplementedException(
            "Wire a Blazor dialog service to implement browser-based approval.");
    }
}
