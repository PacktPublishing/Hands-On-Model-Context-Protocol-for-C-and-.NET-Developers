// Chapter 10 — Section 10.2.2
// MCP server startup with McpAuthenticationDefaults challenge scheme and JWT Bearer
// validation against Microsoft Entra ID. DefaultChallengeScheme serves the
// /.well-known/oauth-protected-resource endpoint so MCP clients auto-discover
// the authorization server. ValidAudience equals the server URL per RFC 8707.

using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

var tenantId  = builder.Configuration["AzureAd:TenantId"]
    ?? throw new InvalidOperationException("AzureAd:TenantId is required.");
var serverUrl = builder.Configuration["McpServer:Url"]
    ?? throw new InvalidOperationException("McpServer:Url is required.");
var authorityUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0";

builder.Services
    .AddAuthentication(options =>
    {
        // Challenge scheme: returns resource metadata on 401 so clients know
        // which authorization server to use and which scopes to request.
        options.DefaultChallengeScheme  = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = authorityUrl;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            // RFC 8707: audience must equal the resource server's own URL,
            // not the client application's ID.
            ValidAudience = serverUrl,
            ValidIssuer   = authorityUrl,
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            AuthorizationServers = { authorityUrl },
            ScopesSupported =
            [
                "travel.flights.read",
                "travel.flights.book",
                "travel.hotels.read",
                "travel.hotels.book",
                "travel.payments.process",
                "travel.itinerary.manage",
            ],
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMcpServer()
    .WithHttpTransport();

var app = builder.Build();

// UseAuthentication must come before UseAuthorization.
// UseAuthorization must come before MapMcp.
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();

app.Run();
