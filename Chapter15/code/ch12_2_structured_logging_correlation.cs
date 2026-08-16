// Chapter 12 — Section 12.1.4
// Structured logging with correlation ID propagation using ILogger.BeginScope.
// BeginScope attaches key-value pairs to every log entry within the scope
// without requiring explicit passing through the call chain.
// CorrelationId originates at the API gateway (Chapter 10) and is forwarded
// through MCP request headers so all services share the same ID.

using Microsoft.Extensions.Logging;

namespace TravelBooking.Flights.Telemetry;

public sealed class CorrelatedToolLogger(ILogger<CorrelatedToolLogger> logger)
{
    public async Task<T> ExecuteAsync<T>(
        string toolName,
        string correlationId,
        string callerId,
        Func<Task<T>> handler)
    {
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["ToolName"]      = toolName,
            ["CallerId"]      = callerId
        }))
        {
            logger.LogInformation("Tool invocation started");
            var start = System.Diagnostics.Stopwatch.GetTimestamp();

            try
            {
                var result = await handler();
                var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
                logger.LogInformation(
                    "Tool invocation completed in {DurationMs}ms",
                    elapsed.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tool invocation failed after {DurationMs}ms",
                    System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                throw;
            }
        }
    }
}

// Middleware that extracts the correlation ID from the MCP request meta or
// from the X-Correlation-Id HTTP header and pushes it into the logger scope
// for every log entry emitted during the request.
public sealed class McpCorrelationScopeMiddleware(
    ILogger<McpCorrelationScopeMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        const string header = "X-Correlation-Id";
        var id = context.Request.Headers[header].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N");

        context.Items[header]          = id;
        context.Response.Headers[header] = id;

        using (logger.BeginScope(new Dictionary<string, object?> { [header] = id }))
            await next(context);
    }
}
