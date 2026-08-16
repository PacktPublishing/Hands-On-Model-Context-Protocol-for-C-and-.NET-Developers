// Chapter 15 — Section 15.7.3
// Safe auto-remediation framework with bounded blast radius.

using Microsoft.Extensions.Logging;

namespace TravelBooking.AgentOps;

public sealed record RemediationAction(
    string Name,
    string Description,
    Func<CancellationToken, Task<bool>> Execute,
    Func<CancellationToken, Task<bool>> VerifySuccess,
    Func<CancellationToken, Task> Rollback,
    TimeSpan MaxBlastDuration,
    int MaxAffectedInstances);

public sealed class SafeRemediator(ILogger<SafeRemediator> logger)
{
    public async Task<bool> AttemptAsync(
        RemediationAction action, CancellationToken ct)
    {
        logger.LogWarning("Attempting remediation: {Name}", action.Name);
        var success = await action.Execute(ct);

        if (!success || !await action.VerifySuccess(ct))
        {
            logger.LogError("Remediation failed, rolling back: {Name}", action.Name);
            await action.Rollback(ct);
            return false;
        }
        logger.LogInformation("Remediation succeeded: {Name}", action.Name);
        return true;
    }
}

// Example: restart a single unhealthy MCP server instance.
public static class RemediationActions
{
    public static RemediationAction RestartInstance(
        string instanceId,
        Func<string, CancellationToken, Task<bool>> restartFunc,
        Func<string, CancellationToken, Task<bool>> healthCheckFunc) =>
        new(
            Name: $"Restart-{instanceId}",
            Description: $"Restart unhealthy instance {instanceId}",
            Execute: ct => restartFunc(instanceId, ct),
            VerifySuccess: ct => healthCheckFunc(instanceId, ct),
            Rollback: _ => Task.CompletedTask,
            MaxBlastDuration: TimeSpan.FromMinutes(2),
            MaxAffectedInstances: 1);

    public static RemediationAction RollbackFeatureFlag(
        string flagName,
        Func<string, bool, CancellationToken, Task> setFlagFunc,
        Func<CancellationToken, Task<bool>> verifyFunc) =>
        new(
            Name: $"Rollback-{flagName}",
            Description: $"Disable feature flag {flagName}",
            Execute: async ct => { await setFlagFunc(flagName, false, ct); return true; },
            VerifySuccess: verifyFunc,
            Rollback: ct => setFlagFunc(flagName, true, ct),
            MaxBlastDuration: TimeSpan.FromMinutes(1),
            MaxAffectedInstances: 0);
}
