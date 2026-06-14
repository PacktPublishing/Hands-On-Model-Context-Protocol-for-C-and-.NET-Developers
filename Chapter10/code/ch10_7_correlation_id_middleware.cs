// Chapter 10 — Section 10.4.3
// Correlation ID middleware that preserves an existing X-Correlation-Id from APIM
// or generates a new one for requests arriving outside the gateway path.
// logger.BeginScope ensures every log entry emitted during the request
// carries the correlation ID without explicit passing through the call chain.
// The companion APIM inbound and outbound policy XML is included below.

namespace TravelBooking.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(
        HttpContext context,
        ILogger<CorrelationIdMiddleware> logger)
    {
        // Preserve the ID injected by APIM; generate one for direct access.
        var id = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[Header]          = id;
        context.Response.Headers[Header] = id;

        using (logger.BeginScope(new Dictionary<string, object> { [Header] = id }))
            await next(context);
    }
}

// Extension method for clean registration in Program.cs:
// app.UseCorrelationId();
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(
        this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}

/*
 * APIM inbound policy — inject correlation ID and validate JWT.
 *
 * <policies>
 *   <inbound>
 *     <base />
 *     <!-- Generate or preserve correlation ID -->
 *     <set-variable name="correlationId"
 *       value="@(context.Request.Headers.GetValueOrDefault(
 *                "X-Correlation-Id", Guid.NewGuid().ToString("N")))" />
 *     <set-header name="X-Correlation-Id" exists-action="override">
 *       <value>@((string)context.Variables["correlationId"])</value>
 *     </set-header>
 *     <!-- Validate JWT before forwarding to MCP server -->
 *     <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
 *       <openid-config url="https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration" />
 *       <audiences><audience>{serverUrl}</audience></audiences>
 *     </validate-jwt>
 *     <!-- Rate limiting: 100 calls per minute per client -->
 *     <rate-limit-by-key calls="100" renewal-period="60"
 *       counter-key="@(context.Subscription?.Id ?? context.Request.IpAddress)" />
 *   </inbound>
 *   <outbound>
 *     <base />
 *     <!-- Echo the correlation ID in the response -->
 *     <set-header name="X-Correlation-Id" exists-action="override">
 *       <value>@((string)context.Variables["correlationId"])</value>
 *     </set-header>
 *     <!-- Strip internal server headers before sending to client -->
 *     <set-header name="X-Powered-By" exists-action="delete" />
 *     <set-header name="Server" exists-action="delete" />
 *   </outbound>
 *   <on-error>
 *     <base />
 *     <set-header name="X-Correlation-Id" exists-action="override">
 *       <value>@((string)context.Variables["correlationId"])</value>
 *     </set-header>
 *   </on-error>
 * </policies>
 */
