// Chapter 12 — Section 12.3.2
// Per-workflow and per-period budget cap enforcement.
// ConcurrentDictionary.AddOrUpdate atomically increments the token count and
// returns the new total so the cap check is coherent under concurrent requests.
// IOptionsMonitor<T> allows cap values to be updated at runtime (e.g. from Key Vault)
// without restarting the server — the same zero-downtime pattern as Chapter 10.

using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace TravelBooking.Orchestrator.Budget;

public sealed class BudgetCapOptions
{
    public long WorkflowCapTokens    { get; set; } = 5_000;
    public long TenantPeriodCapTokens { get; set; } = 500_000;
}

public sealed class BudgetExceededException(string message) : Exception(message);

public sealed class BudgetCapEnforcer(IOptionsMonitor<BudgetCapOptions> options)
{
    private readonly ConcurrentDictionary<string, long> _workflowUsage = new();
    private readonly ConcurrentDictionary<string, long> _periodUsage   = new();

    public void Enforce(string workflowId, string tenantId, long tokens)
    {
        var workflowTotal = _workflowUsage.AddOrUpdate(
            workflowId, tokens, (_, prev) => prev + tokens);

        if (workflowTotal > options.CurrentValue.WorkflowCapTokens)
            throw new BudgetExceededException(
                $"Workflow {workflowId} exceeded the " +
                $"{options.CurrentValue.WorkflowCapTokens:N0}-token cap " +
                $"(accumulated: {workflowTotal:N0}).");

        var periodTotal = _periodUsage.AddOrUpdate(
            tenantId, tokens, (_, prev) => prev + tokens);

        if (periodTotal > options.CurrentValue.TenantPeriodCapTokens)
            throw new BudgetExceededException(
                $"Tenant {tenantId} exceeded the hourly " +
                $"{options.CurrentValue.TenantPeriodCapTokens:N0}-token budget.");
    }

    // Call at the start of each period (hourly) to reset per-period counters.
    // In production, a hosted service drives this on a timer.
    public void ResetPeriodCounters() => _periodUsage.Clear();

    // Remove a workflow's counter when it completes normally.
    public void CompleteWorkflow(string workflowId) => _workflowUsage.TryRemove(workflowId, out _);
}

// DI registration:
// builder.Services
//     .AddOptions<BudgetCapOptions>()
//     .BindConfiguration("Budget")
//     .ValidateDataAnnotations();
// builder.Services.AddSingleton<BudgetCapEnforcer>();
