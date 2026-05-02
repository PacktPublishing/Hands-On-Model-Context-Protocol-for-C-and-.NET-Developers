// Chapter 6 — Section 6.4.4
// McpErrorSanitisationFilter: decides which client-facing message to produce
// for each exception type that escapes a tool handler.
//
//   - McpException messages are authored for client consumption and pass through as-is.
//   - Known domain exceptions (e.g. FlightNotAvailableException) use a safe generic
//     message with a correlation ID for support lookup.
//   - All other exceptions fall to the default branch — never exposing Exception.Message.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;

namespace TravelBooking.Chapter06;

public interface ICorrelationIdProvider
{
    string NewId();
}

/// <summary>Generates a short uppercase hex ID suitable for both log entries and error responses.</summary>
public sealed class GuidCorrelationIdProvider : ICorrelationIdProvider
{
    public string NewId() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}

/// <summary>
/// Maps any exception that escapes a tool handler to a sanitised, client-safe message.
/// Logs the full exception when the original is not an <see cref="McpException"/>.
/// </summary>
public sealed class McpErrorSanitisationFilter
{
    private readonly ICorrelationIdProvider _correlationIds;
    private readonly ILogger<McpErrorSanitisationFilter> _logger;

    public McpErrorSanitisationFilter(
        ICorrelationIdProvider correlationIds,
        ILogger<McpErrorSanitisationFilter>? logger = null)
    {
        _correlationIds = correlationIds;
        _logger = logger ?? NullLogger<McpErrorSanitisationFilter>.Instance;
    }

    public string GetClientMessage(Exception ex)
    {
        var correlationId = _correlationIds.NewId();
        var message = GetClientMessage(ex, correlationId);

        // Log everything except McpException — those are authored errors, not unexpected ones.
        if (ex is not McpException)
            _logger.LogError(ex, "Unhandled exception. Correlation ID: {CorrelationId}", correlationId);

        return message;
    }

    /// <summary>Static overload — useful in tests and stateless middleware.</summary>
    public static string GetClientMessage(Exception ex, string correlationId) => ex switch
    {
        // McpException carries a message explicitly written for the LLM host — pass it through.
        McpException mcp => mcp.Message,

        // Known domain exceptions use a safe generic form with a correlation ID.
        FlightNotAvailableException
            => $"The requested flight is not available. ({correlationId})",

        // Everything else — database errors, NullReferenceExceptions, timeouts that escaped Polly —
        // must never expose their Message to the client.
        _ => $"An unexpected error occurred. Reference: {correlationId}",
    };
}

/// <summary>
/// Wraps a tool-handler delegate and converts any escaping exception into an
/// <see cref="McpException"/> carrying the sanitised message.
/// </summary>
public sealed class SanitisedToolInvoker
{
    private readonly McpErrorSanitisationFilter _filter;

    public SanitisedToolInvoker(McpErrorSanitisationFilter filter) => _filter = filter;

    public async Task<T> InvokeAsync<T>(
        Func<CancellationToken, Task<T>> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return await handler(cancellationToken);
        }
        catch (Exception ex)
        {
            var clientMessage = _filter.GetClientMessage(ex);
            throw new McpException(clientMessage);
        }
    }
}
