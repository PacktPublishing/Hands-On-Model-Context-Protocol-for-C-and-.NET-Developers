// Chapter 6 — Section 6.4.4
// McpErrorSanitisationFilter: decides which client-facing message to produce
// for each exception type that escapes a tool handler.
// McpException messages are authored for client consumption and pass through as-is.
// Domain exceptions use a safe generic message with a correlation ID for support lookup.
// All other exceptions fall to the default branch — never exposing Exception.Message.

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.FlightsServer.Errors;

public interface ICorrelationIdProvider
{
    string NewId();
}

// Generates a short uppercase hex ID suitable for both log entries and error responses.
public sealed class GuidCorrelationIdProvider : ICorrelationIdProvider
{
    public string NewId() => Guid.NewGuid().ToString("N")[..12].ToUpper();
}

public sealed class McpErrorSanitisationFilter
{
    private readonly ICorrelationIdProvider _correlationIds;
    private readonly ILogger<McpErrorSanitisationFilter> _logger;

    public McpErrorSanitisationFilter(
        ICorrelationIdProvider correlationIds,
        ILogger<McpErrorSanitisationFilter> logger)
    {
        _correlationIds = correlationIds;
        _logger = logger;
    }

    public string GetClientMessage(Exception ex)
    {
        var correlationId = _correlationIds.NewId();
        var message = GetClientMessage(ex, correlationId);

        // Log everything except McpException — those are authored errors, not unexpected ones.
        if (ex is not McpException)
            _logger.LogError(ex,
                "Unhandled exception. Correlation ID: {CorrelationId}", correlationId);

        return message;
    }

    // Static overload for use in tests and the output sanitisation middleware.
    public static string GetClientMessage(
        Exception ex, string correlationId) => ex switch
    {
        // McpException carries a message explicitly written for the LLM host — pass it through.
        McpException mcp => mcp.Message,

        // Known domain exceptions use a safe generic form with a correlation ID.
        FlightNotAvailableException
            => $"The requested flight is not available. ({correlationId})",

        // Everything else — database errors, NullReferenceExceptions, timeouts that escaped Polly —
        // must never expose their Message to the client.
        _ => $"An unexpected error occurred. Reference: {correlationId}"
    };
}

// Middleware wrapper that applies sanitisation around any tool handler invocation.
// Register as a DI service and call InvokeAsync from each [McpServerTool] method body.
public sealed class SanitisedToolInvoker
{
    private readonly McpErrorSanitisationFilter _filter;

    public SanitisedToolInvoker(McpErrorSanitisationFilter filter)
        => _filter = filter;

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

// DI registration — add to Program.cs alongside AddMcpServer().
public static class SanitisationExtensions
{
    public static IServiceCollection AddErrorSanitisation(
        this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationIdProvider, GuidCorrelationIdProvider>();
        services.AddSingleton<McpErrorSanitisationFilter>();
        services.AddSingleton<SanitisedToolInvoker>();
        return services;
    }
}
