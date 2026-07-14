// Chapter 10 — Section 10.2
// Structural contracts for specialist agents: HandoffToken, AgentResult, ISpecialistAgent.
// All four specialist agents implement ISpecialistAgent; the coordinator routes via AgentId.

namespace TravelBooking.MultiAgent;

/// <summary>
/// Carries the task and prior context from the coordinator to a specialist agent.
/// PriorContext should be structured JSON when the receiving agent needs to parse it.
/// </summary>
public sealed record HandoffToken(
    string SessionId,
    string AgentId,
    string Task,
    string? PriorContext = null);

/// <summary>
/// The output a specialist agent returns to the coordinator.
/// RequiresEscalation = true halts the coordinator and raises the session to the user.
/// </summary>
public sealed record AgentResult(
    string AgentId,
    string Output,
    bool RequiresEscalation = false,
    string? EscalationReason = null)
{
    public string SessionId() =>
        string.Empty; // coordinator injects session context when constructing tokens
}

/// <summary>
/// Contract every specialist agent must satisfy.
/// Each implementation owns its own IChatClient, McpClient, and system prompt.
/// </summary>
public interface ISpecialistAgent
{
    string AgentId { get; }

    Task<AgentResult> RunAsync(
        HandoffToken handoff,
        CancellationToken ct = default);
}

/// <summary>
/// Result returned by AgentCoordinator.RunAsync.
/// </summary>
public sealed record CoordinatorResult(
    bool IsEscalation,
    AgentResult FinalResult)
{
    public static CoordinatorResult EscalationRequired(AgentResult r) =>
        new(true, r);

    public static CoordinatorResult Completed(AgentResult r) =>
        new(false, r);
}
