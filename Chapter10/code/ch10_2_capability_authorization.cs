// Chapter 10 — Section 10.2.3
// Capability-level authorization mapping MCP tool names to required OAuth scopes.
// HasCapability checks the caller's JWT scp claim against the required scope
// for a named tool. Unknown tools are denied by default (fail-closed).
// McpToolScopeHandler integrates this check into ASP.NET Core authorization policy.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TravelBooking.Security;

public static class ToolScopes
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["search_flights"]      = "travel.flights.read",
        ["get_flight_status"]   = "travel.flights.read",
        ["book_flight"]         = "travel.flights.book",
        ["select_seat"]         = "travel.flights.book",
        ["search_hotels"]       = "travel.hotels.read",
        ["check_availability"]  = "travel.hotels.read",
        ["book_hotel"]          = "travel.hotels.book",
        ["upgrade_room"]        = "travel.hotels.book",
        ["process_payment"]     = "travel.payments.process",
        ["issue_refund"]        = "travel.payments.process",
        ["create_itinerary"]    = "travel.itinerary.manage",
        ["update_itinerary"]    = "travel.itinerary.manage",
    };

    // Returns false for unknown tools — deny by default.
    public static bool HasCapability(ClaimsPrincipal user, string toolName)
    {
        if (!Map.TryGetValue(toolName, out var requiredScope))
            return false;

        return user.FindAll("scp")
                   .SelectMany(c => c.Value.Split(' '))
                   .Contains(requiredScope);
    }

    public static string? RequiredScope(string toolName) =>
        Map.TryGetValue(toolName, out var scope) ? scope : null;
}

// Integrates ToolScopes.HasCapability into the ASP.NET Core authorization pipeline.
// Register with: services.AddSingleton<IAuthorizationHandler, McpToolScopeHandler>();
// Use with: services.AddAuthorization(o => o.AddPolicy("McpTool",
//     p => p.Requirements.Add(new McpToolScopeRequirement())));
public sealed record McpToolScopeRequirement : IAuthorizationRequirement;

public sealed class McpToolScopeHandler
    : AuthorizationHandler<McpToolScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        McpToolScopeRequirement requirement)
    {
        // The tool name is passed as the resource when calling IAuthorizationService.
        if (context.Resource is not string toolName)
        {
            context.Fail(new AuthorizationFailureReason(this,
                "Tool name resource not provided."));
            return Task.CompletedTask;
        }

        if (ToolScopes.HasCapability(context.User, toolName))
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this,
                $"Token does not carry required scope for tool '{toolName}'."));

        return Task.CompletedTask;
    }
}
