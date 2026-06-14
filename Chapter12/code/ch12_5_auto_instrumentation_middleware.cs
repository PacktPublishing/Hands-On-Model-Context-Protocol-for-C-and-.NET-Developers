// Chapter 12 — Section 12.2.2
// Auto-instrumentation middleware that wraps every MCP tool call with
// standardised span attributes and operation duration recording.
// TryGetOuterToolExecutionActivity checks whether outer GenAI instrumentation
// (e.g. Semantic Kernel or MEAI) is already tracing the tool call; if so,
// this middleware adds MCP-specific attributes to the existing span rather
// than creating a duplicate, per the OTel semantic conventions for MCP.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TravelBooking.Telemetry;

public sealed class McpToolInstrumentationMiddleware(
    McpServerMetrics metrics,
    ILogger<McpToolInstrumentationMiddleware> logger)
{
    private static readonly ActivitySource Source =
        new("TravelBooking.McpServer", "1.0.0");

    public async Task<CallToolResult> InvokeAsync(
        McpToolContext context,
        Func<McpToolContext, Task<CallToolResult>> next)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        // Reuse an outer span from GenAI orchestration if present; create one otherwise.
        Activity? ownActivity = null;
        if (!Diagnostics.TryGetOuterToolExecutionActivity(out var activity))
        {
            ownActivity = Source.StartActivity(
                $"mcp.tool.{context.ToolName}", ActivityKind.Server);
            activity = ownActivity;
        }

        activity?.SetTag("mcp.tool.name",   context.ToolName);
        activity?.SetTag("mcp.server.name", context.ServerName);
        activity?.SetTag("mcp.caller.id",   context.CallerId);

        try
        {
            var result = await next(context);
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);

            activity?.SetTag("mcp.tool.status", result.IsError ? "error" : "ok");
            activity?.SetStatus(result.IsError
                ? ActivityStatusCode.Error : ActivityStatusCode.Ok);

            metrics.RecordInvocation(
                context.ToolName, elapsed.TotalMilliseconds, result.IsError);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            ownActivity?.Dispose();
        }
    }
}

// Minimal context type for compilation; matches the actual MCP middleware pattern.
public record McpToolContext(string ToolName, string ServerName, string CallerId);

// Forward reference to avoid cross-file dependency; matches the SDK's Diagnostics class.
file static class Diagnostics
{
    public static bool TryGetOuterToolExecutionActivity(out Activity? activity)
    {
        if (Activity.Current is { } current &&
            current.OperationName.StartsWith("execute_tool ", StringComparison.Ordinal))
        {
            activity = current;
            return true;
        }
        activity = null;
        return false;
    }
}
