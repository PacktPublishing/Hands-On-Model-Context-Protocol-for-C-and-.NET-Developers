// Chapter 10 — Section 10.4.3
// EscalationHandler: persists escalation to the shared MCP server, notifies the user,
// and returns a record the coordinator uses to halt the workflow until resumed.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.MultiAgent;

public enum EscalationStatus
{
    PendingUserResponse,
    UserResponded,
    Cancelled
}

public sealed record EscalationRecord(
    string SessionId,
    string Reason,
    DateTimeOffset RaisedAt,
    EscalationStatus Status);

/// <summary>
/// Abstraction over the user notification channel (push notification, webhook, email).
/// Implementations are registered in DI and injected into EscalationHandler.
/// </summary>
public interface IUserNotifier
{
    Task NotifyUserAsync(EscalationRecord record, CancellationToken ct);
}

public sealed class EscalationHandler(
    McpClient sharedMcpClient,
    IUserNotifier notifier,
    ILogger<EscalationHandler> logger)
{
    public async Task<EscalationRecord> EscalateAsync(
        string sessionId,
        string reason,
        CancellationToken ct = default)
    {
        var record = new EscalationRecord(
            sessionId,
            reason,
            DateTimeOffset.UtcNow,
            EscalationStatus.PendingUserResponse);

        logger.LogWarning(
            "Escalating session {Id}: {Reason}", sessionId, reason);

        // Persist so the workflow can resume after a server restart.
        var saveResult = await sharedMcpClient.CallToolAsync(
            "record_escalation",
            new Dictionary<string, object?>
            {
                ["session_id"] = sessionId,
                ["reason"]     = reason,
                ["status"]     = "pending",
                ["raised_at"]  = record.RaisedAt.ToString("O")
            }, ct: ct);

        if (saveResult.IsError is true)
        {
            var error = saveResult.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()?.Text ?? "record_escalation failed.";
            logger.LogError(
                "Failed to persist escalation for session {Id}: {Error}",
                sessionId, error);
        }

        await notifier.NotifyUserAsync(record, ct);
        return record;
    }

    public async Task<EscalationRecord?> LoadAsync(
        string sessionId, CancellationToken ct = default)
    {
        var resource = await sharedMcpClient.ReadResourceAsync(
            $"escalation://{sessionId}", ct);
        var json = resource.Contents
            .OfType<TextResourceContents>()
            .FirstOrDefault()?.Text;
        if (string.IsNullOrEmpty(json))
            return null;
        return System.Text.Json.JsonSerializer
            .Deserialize<EscalationRecord>(json);
    }
}
