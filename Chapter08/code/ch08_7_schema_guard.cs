// Chapter 8 — Section 8.3.3
// SchemaGuard validates the tool name against the server's live capability list.
// Catches hallucinated tool names before the network round-trip to the MCP server.
// Refresh the tool dictionary when ToolListChangedNotification arrives.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Orchestration.Guardrails;

public sealed class SchemaGuard(
    IReadOnlyDictionary<string, McpClientTool> tools)
    : IGuardrail
{
    public Task<GuardrailResult> CheckAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken ct = default)
    {
        if (!tools.ContainsKey(toolName))
            return Task.FromResult(GuardrailResult.Reject(
                $"Unknown tool '{toolName}'. " +
                $"Available tools: {string.Join(", ", tools.Keys)}."));

        // Validate required arguments against the tool's InputSchema.
        var tool = tools[toolName];
        var schema = tool.ProtocolTool.InputSchema;
        if (schema.Required is { } required)
        {
            var missing = required
                .Where(r => !args.ContainsKey(r) || args[r] is null)
                .ToList();
            if (missing.Count > 0)
                return Task.FromResult(GuardrailResult.Reject(
                    $"Tool '{toolName}' is missing required arguments: " +
                    string.Join(", ", missing)));
        }

        return Task.FromResult(GuardrailResult.Allow());
    }

    // Factory method — call after every ListToolsAsync to keep the guard current.
    public static SchemaGuard FromTools(IList<McpClientTool> tools) =>
        new(tools.ToDictionary(t => t.Name));
}
