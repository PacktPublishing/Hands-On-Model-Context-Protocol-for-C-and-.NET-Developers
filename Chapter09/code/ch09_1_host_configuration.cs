// Chapter 9 — Section 9.1.1
// Blazor Server host configuration with MCP client and IChatClient registration.
// AddRazorComponents + AddInteractiveServerComponents enables SignalR-backed rendering.
// MCP client and IChatClient registrations from Chapters 7 and 8 carry forward unchanged.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using TravelBooking.Blazor.Services;
using TravelBooking.Orchestration;
using TravelBooking.Orchestration.Guardrails;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server rendering pipeline ────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── MCP clients — one per server, scoped per Blazor circuit ─────────────────
builder.Services.AddSingleton<McpClient>(_ =>
    McpClient.CreateAsync(
        new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(builder.Configuration["Mcp:FlightsEndpoint"]!),
            Name = "FlightsServer"
        }),
        new McpClientOptions
        {
            ClientInfo = new ModelContextProtocol.Protocol.Implementation
            {
                Name = "TravelBooking.Web", Version = "1.0"
            }
        }).GetAwaiter().GetResult());

// ── LLM provider (Chapter 8) ─────────────────────────────────────────────────
// Replace AnthropicClient with your provider; model version must be pinned.
builder.Services.AddSingleton<IChatClient>(_ =>
    new AnthropicClient().AsChatClient("claude-sonnet-4-6"));

// ── Orchestration and guardrails (Chapter 8) ─────────────────────────────────
builder.Services.AddSingleton<IGuardrail, PromptInjectionGuardrail>();
builder.Services.AddSingleton<IGuardrail, SchemaGuard>(_ =>
    SchemaGuard.FromTools([]));  // refreshed per-session via ListToolsAsync
builder.Services.AddSingleton<IGuardrail, ApprovalGate>();
builder.Services.AddSingleton<GuardrailPipeline>(sp =>
    new GuardrailPipeline(sp.GetServices<IGuardrail>().ToList()));
builder.Services.AddSingleton<IApprovalProvider, BlazorApprovalProvider>();
builder.Services.AddScoped<ReActOrchestrator>();
builder.Services.AddSingleton<AuditLogger>();
builder.Services.AddSingleton<OrchestratorMetrics>();

// ── Background job infrastructure (Section 9.2) ──────────────────────────────
builder.Services.AddSingleton<McpJobQueue>();
builder.Services.AddSingleton<JobStatusStore>();
builder.Services.AddHostedService<JobProcessorService>();

// ── Caching and offline support (Section 9.3) ────────────────────────────────
builder.Services.AddSingleton<IConnectivityService, PollingConnectivityService>();
builder.Services.AddScoped<CachingMcpClient>();
builder.Services.AddHttpClient<PollingConnectivityService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Placeholder for the root App component reference.
internal sealed class App;
