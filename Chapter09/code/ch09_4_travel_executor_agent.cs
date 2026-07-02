// Chapter 9 (Replacement) — Section 9.2.3
// TravelExecutorAgent: iterates a TravelPlan and dispatches each step to the MCP server.
// Enforces transition guards, budget caps, approval checkpoints, and compensation on failure.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Agentic;

public sealed class TravelExecutorAgent(
    McpClient mcpClient,
    IApprovalProvider approvals,
    TransitionGuard guard,
    WorkflowBudget budget,
    WorkflowStateStore stateStore,
    ILogger<TravelExecutorAgent> logger)
{
    public async Task<ExecutionResult> ExecuteAsync(
        string workflowId,
        TravelPlan plan,
        CancellationToken ct = default)
    {
        var toolMap = (await mcpClient.ListToolsAsync(ct))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        var currentState = await stateStore.LoadAsync(workflowId, ct)
            ?? new IdleState();

        var results = new List<StepResult>();
        var compensations = new Stack<WorkflowStep>();

        foreach (var step in plan.Steps)
        {
            if (step.RequiresApproval)
            {
                var approved = await approvals.RequestApprovalAsync(
                    step.ToolName, step.Args, ct);
                if (!approved)
                    return ExecutionResult.Cancelled(results);
            }

            try
            {
                guard.AssertAllowed(currentState, step.ToolName);
                budget.Consume(step.ToolName);

                if (!toolMap.TryGetValue(step.ToolName, out var tool))
                    throw new InvalidOperationException(
                        $"Tool '{step.ToolName}' not found.");

                var rawResult = await tool.InvokeAsync(step.Args, ct);
                results.Add(StepResult.Success(step, rawResult));

                if (step.IsReversible)
                    compensations.Push(step);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "Step '{Tool}' failed; running compensation",
                    step.ToolName);
                await CompensateAsync(workflowId, compensations, toolMap, ct);
                return ExecutionResult.Failed(step, ex.Message, results);
            }
        }

        return ExecutionResult.Completed(results);
    }

    private async Task CompensateAsync(
        string workflowId,
        Stack<WorkflowStep> compensations,
        Dictionary<string, McpClientTool> toolMap,
        CancellationToken ct)
    {
        while (compensations.TryPop(out var step))
        {
            if (step.CompensationTool is null) continue;
            if (!toolMap.TryGetValue(step.CompensationTool, out var undo)) continue;

            try
            {
                await undo.InvokeAsync(step.Args, ct);
                logger.LogInformation(
                    "Compensation '{Tool}' succeeded for step '{Step}'",
                    step.CompensationTool, step.ToolName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Compensation '{Tool}' failed; leaving side effect unresolved",
                    step.CompensationTool);
            }
        }
    }
}
