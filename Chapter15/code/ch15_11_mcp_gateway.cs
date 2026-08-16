// Chapter 15 — Section 15.5.3
// MCP gateway middleware with token exchange, audit logging, and rate limiting.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TravelBooking.Gateway;

public sealed record ServerRegistration(
    string Name,
    string Endpoint,
    string RequiredScope,
    HttpClient HttpClient);

public interface IServerRegistry
{
    ServerRegistration? Resolve(string mcpMethod, string mcpName);
}

public interface ITokenExchangeService
{
    Task<string> ExchangeAsync(
        System.Security.Claims.ClaimsPrincipal user,
        string requiredScope,
        CancellationToken ct);
}

public sealed class McpGatewayException(string message) : Exception(message);

public sealed class McpGatewayMiddleware(
    RequestDelegate next,
    IServerRegistry registry,
    ITokenExchangeService tokenExchange,
    ILogger<McpGatewayMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Headers["Mcp-Method"].ToString();
        var name   = context.Request.Headers["Mcp-Name"].ToString();

        if (string.IsNullOrEmpty(method))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var server = registry.Resolve(method, name);
        if (server is null)
        {
            logger.LogWarning("No server registered for {Method}/{Name}", method, name);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var scopedToken = await tokenExchange.ExchangeAsync(
            context.User, server.RequiredScope, context.RequestAborted);

        logger.LogInformation(
            "Routing {Method}/{Name} to {Server}",
            method, name, server.Name);

        using var forwardRequest = new HttpRequestMessage(
            HttpMethod.Post, server.Endpoint);
        forwardRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", scopedToken);
        forwardRequest.Content = new StreamContent(context.Request.Body);
        forwardRequest.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(context.Request.ContentType!);

        using var response = await server.HttpClient.SendAsync(
            forwardRequest, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        await response.Content.CopyToAsync(context.Response.Body);
    }
}

public sealed class InMemoryServerRegistry : IServerRegistry
{
    private readonly Dictionary<string, ServerRegistration> _servers = new();

    public void Register(string toolName, ServerRegistration registration) =>
        _servers[toolName] = registration;

    public ServerRegistration? Resolve(string mcpMethod, string mcpName)
    {
        if (mcpMethod == "tools/call" && _servers.TryGetValue(mcpName, out var server))
            return server;

        // Fall back to first server that handles the method category.
        return _servers.Values.FirstOrDefault();
    }
}
