// Chapter 12 — Section 12.4.5
// Priority-based load shedding middleware.
// Rejects low-priority requests with HTTP 503 + Retry-After when active
// request count exceeds the threshold for that priority tier.
// Critical operations (process_payment, book_flight in progress) are never shed.
// The finally block always decrements the counter so the load estimate stays accurate.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace TravelBooking.Middleware;

public enum RequestPriority { Critical, High, Standard, BestEffort }

public sealed class LoadSheddingOptions
{
    public int Tier1Threshold { get; set; } = 200; // Shed BestEffort above 200
    public int Tier2Threshold { get; set; } = 400; // Shed Standard above 400
    public int Tier3Threshold { get; set; } = 600; // Shed High above 600
    // Critical: never shed
}

public sealed class LoadSheddingMiddleware(
    RequestDelegate next,
    IOptionsMonitor<LoadSheddingOptions> options)
{
    private int _activeRequests;

    public async Task InvokeAsync(HttpContext context)
    {
        var priority = ClassifyRequest(context.Request.Path);
        var active   = Interlocked.Increment(ref _activeRequests);

        try
        {
            if (ShouldShed(priority, active, options.CurrentValue))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.RetryAfter = "30";
                return;
            }
            await next(context);
        }
        finally
        {
            Interlocked.Decrement(ref _activeRequests);
        }
    }

    private static bool ShouldShed(
        RequestPriority priority, int active, LoadSheddingOptions opts) =>
        priority switch
        {
            RequestPriority.BestEffort => active > opts.Tier1Threshold,
            RequestPriority.Standard   => active > opts.Tier2Threshold,
            RequestPriority.High       => active > opts.Tier3Threshold,
            RequestPriority.Critical   => false,
            _                          => false
        };

    private static RequestPriority ClassifyRequest(PathString path)
    {
        if (path.StartsWithSegments("/mcp/tools/process_payment") ||
            path.StartsWithSegments("/mcp/tools/book_flight"))
            return RequestPriority.Critical;

        if (path.StartsWithSegments("/mcp/tools/book_hotel"))
            return RequestPriority.High;

        if (path.StartsWithSegments("/mcp"))
            return RequestPriority.Standard;

        return RequestPriority.BestEffort;
    }
}

// Extension for clean registration:
//   app.UseMiddleware<LoadSheddingMiddleware>();
// Or:
//   app.UseLoadShedding();
public static class LoadSheddingExtensions
{
    public static IApplicationBuilder UseLoadShedding(this IApplicationBuilder app) =>
        app.UseMiddleware<LoadSheddingMiddleware>();
}
