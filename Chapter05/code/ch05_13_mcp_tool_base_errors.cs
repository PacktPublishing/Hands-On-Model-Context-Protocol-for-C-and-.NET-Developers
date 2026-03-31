// Chapter 5 — Section 5.3.4
// Error handling in tool handlers using McpException for business errors
// and McpProtocolException for protocol-level failures.
//
// McpException(string message)            — business/validation errors; message sent to client
// McpProtocolException(message, errorCode) — protocol errors; use valid McpErrorCode values only
//
// Valid McpErrorCode values: ResourceNotFound, UrlElicitationRequired, InvalidRequest,
//   MethodNotFound, InvalidParams, InternalError, ParseError
//
// Do NOT use: RequestTimeout, RequestCancelled, ServerError, Conflict — they do not exist.

using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed partial class FlightTools
{
    [McpServerTool, Description("Cancel an existing flight booking and request a refund.")]
    public async Task<CancellationResult> CancelFlight(
        [Description("Booking reference returned by BookFlight")] string bookingReference,
        [Description("Reason for cancellation")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookingReference))
            // Use McpProtocolException for protocol-level input validation failures
            throw new McpProtocolException(
                "Booking reference must not be empty.",
                McpErrorCode.InvalidParams);

        var booking = await _bookingService.GetAsync(bookingReference, cancellationToken)
            ?? throw new McpProtocolException(
                $"Booking '{bookingReference}' was not found.",
                McpErrorCode.ResourceNotFound);

        if (booking.Status == "cancelled")
            // Use McpException for business rule violations
            throw new McpException(
                $"Booking '{bookingReference}' is already cancelled.");

        return await _bookingService.CancelAsync(bookingReference, reason, cancellationToken);
    }
}
