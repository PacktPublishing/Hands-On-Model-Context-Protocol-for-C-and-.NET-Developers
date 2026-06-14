// Chapter 11 — Section 11.3.1
// Azure Functions v4 isolated process host for an MCP server.
// ConfigureFunctionsWebApplication enables the full ASP.NET Core middleware pipeline
// inside the worker process, so UseAuthentication, UseAuthorization, and MapMcp
// work exactly as they do in a standard ASP.NET Core host.
// ConfigureFunctionsWorkerDefaults does NOT enable this pipeline — use WebApplication instead.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore.Authentication;
using TravelBooking.Flights.Capabilities;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(app =>
    {
        // ASP.NET Core middleware executes in the worker process before Functions triggers.
        app.UseAuthentication();
        app.UseAuthorization();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var tenantId  = ctx.Configuration["Entra:TenantId"]!;
        var serverUrl = ctx.Configuration["Mcp:ServerUrl"]!;
        var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

        services
            .AddAuthentication(opts =>
            {
                // DefaultChallengeScheme serves /.well-known/oauth-protected-resource.
                opts.DefaultChallengeScheme  = McpAuthenticationDefaults.AuthenticationScheme;
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.Authority = authority;
                // RFC 8707: audience must equal the server's own URL, not the client app ID.
                opts.TokenValidationParameters = new() { ValidAudience = serverUrl };
            })
            .AddMcp(opts => opts.ResourceMetadata = new()
            {
                AuthorizationServers = { authority },
                ScopesSupported = ["travel.flights.read"],
            });

        services.AddAuthorization();

        services
            .AddMcpServer()
            .WithHttpTransport()
            .AddFlightSearchCapabilities();
    })
    .Build();

host.Run();
