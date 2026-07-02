// Chapter 9 (Replacement) — Section 9.5.2
// TransitionGuard: enforces the allowed tool-call set for each workflow state.
// Throws InvalidTransitionException before dispatch when the tool is not permitted.

namespace TravelBooking.Agentic;

public sealed class TransitionGuard
{
    // Tools permitted in each state. ReadOnly tools (get_itinerary) are allowed
    // in all states via the wildcard entry; add them to every state that needs them.
    private static readonly Dictionary<Type, string[]> Allowed = new()
    {
        [typeof(IdleState)]             = [],
        [typeof(SearchingState)]        = ["search_flights"],
        [typeof(ComparingState)]        = ["search_flights"],
        [typeof(ReservingState)]        = ["reserve_flight"],
        [typeof(AwaitingApprovalState)] = ["book_flight"],
        [typeof(ConfirmedState)]        = ["cancel_flight", "get_itinerary"],
        [typeof(FailedState)]           = [],
        [typeof(CancelledState)]        = [],
    };

    // Read-only diagnostic tools allowed in all states.
    private static readonly HashSet<string> AlwaysAllowed =
        ["get_itinerary", "list_airports"];

    public void AssertAllowed(WorkflowState state, string toolName)
    {
        if (AlwaysAllowed.Contains(toolName)) return;

        if (!Allowed.TryGetValue(state.GetType(), out var tools)
            || !tools.Contains(toolName, StringComparer.Ordinal))
            throw new InvalidTransitionException(
                $"Tool '{toolName}' is not valid in state " +
                $"'{state.GetType().Name}'. " +
                $"Allowed: [{string.Join(", ", tools ?? [])}].");
    }
}

public sealed class InvalidTransitionException(string message) : Exception(message);
