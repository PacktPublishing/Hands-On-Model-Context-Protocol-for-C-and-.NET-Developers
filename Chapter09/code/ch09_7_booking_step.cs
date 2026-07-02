// Chapter 9 (Replacement) — Section 9.3.3
// BookingStepHandler: calls book_flight with retry for transient failures,
// idempotency detection for duplicate bookings, and state transition on every outcome.

using ModelContextProtocol.Client;

namespace TravelBooking.Agentic;

public sealed class BookingStepHandler(
    McpClient mcpClient,
    WorkflowStateStore stateStore,
    ILogger<BookingStepHandler> logger)
{
    private const int MaxAttempts = 3;

    public async Task<ExecutionResult> ExecuteAsync(
        string workflowId,
        string reservationId,
        string paymentToken,
        CancellationToken ct = default)
    {
        var tools = (await mcpClient.ListToolsAsync(ct))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        var args = new Dictionary<string, object?>
        {
            ["reservation_id"]  = reservationId,
            ["payment_token"]   = paymentToken
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var bookTool = tools["book_flight"];
                var rawResult = await bookTool.InvokeAsync(args, ct);
                var bookingRef = rawResult?.ToString()!;

                await stateStore.TransitionAsync(
                    workflowId, new ConfirmedState(bookingRef), ct);

                logger.LogInformation(
                    "Booking confirmed: {Ref} (attempt {N})", bookingRef, attempt);
                return ExecutionResult.Completed(bookingRef);
            }
            catch (McpException ex) when (IsTransient(ex) && attempt < MaxAttempts)
            {
                logger.LogWarning(ex,
                    "Transient failure on attempt {N}; retrying", attempt);
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
            catch (McpException ex) when (IsDuplicate(ex))
            {
                var existingRef = ExtractExistingRef(ex);
                await stateStore.TransitionAsync(
                    workflowId, new ConfirmedState(existingRef), ct);
                return ExecutionResult.Completed(existingRef);
            }
            catch (Exception ex)
            {
                await stateStore.TransitionAsync(workflowId,
                    new FailedState(ex.Message, attempt), ct);
                return ExecutionResult.Failed("booking", ex.Message, []);
            }
        }

        // All attempts exhausted
        await stateStore.TransitionAsync(workflowId,
            new FailedState("Max retry attempts exhausted.", MaxAttempts), ct);
        return ExecutionResult.Failed(
            "booking", "Max retry attempts exhausted.", []);
    }

    private static bool IsTransient(McpException ex) =>
        ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("gateway", StringComparison.OrdinalIgnoreCase);

    private static bool IsDuplicate(McpException ex) =>
        ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("already booked", StringComparison.OrdinalIgnoreCase);

    private static string ExtractExistingRef(McpException ex)
    {
        // Parse the booking reference from the duplicate-booking error payload.
        // Convention: "Duplicate booking: BKG-XXXXX"
        var parts = ex.Message.Split(' ');
        return parts.Length > 2 ? parts[^1] : "UNKNOWN";
    }
}
