// Chapter 9 (Replacement) — Section 9.1
// Discriminated-union workflow state hierarchy for the Travel Booking agentic workflow.
// Each record carries exactly the data its transitions need; invalid states are unrepresentable.

using System.Text.Json.Serialization;

namespace TravelBooking.Agentic;

// ---------------------------------------------------------------------------
// Shared value types
// ---------------------------------------------------------------------------

public sealed record FlightOption(
    string FlightId,
    string Airline,
    string Origin,
    string Destination,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    decimal Price,
    string Currency,
    int StopsCount);

// ---------------------------------------------------------------------------
// Workflow state hierarchy
// ---------------------------------------------------------------------------

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$state")]
[JsonDerivedType(typeof(IdleState),             "idle")]
[JsonDerivedType(typeof(SearchingState),        "searching")]
[JsonDerivedType(typeof(ComparingState),        "comparing")]
[JsonDerivedType(typeof(ReservingState),        "reserving")]
[JsonDerivedType(typeof(AwaitingApprovalState), "awaiting_approval")]
[JsonDerivedType(typeof(ConfirmedState),        "confirmed")]
[JsonDerivedType(typeof(FailedState),           "failed")]
[JsonDerivedType(typeof(CancelledState),        "cancelled")]
public abstract record WorkflowState;

public sealed record IdleState : WorkflowState;

public sealed record SearchingState(
    string Origin,
    string Destination,
    DateOnly DepartureDate) : WorkflowState;

public sealed record ComparingState(
    FlightOption[] Options,
    string SearchId) : WorkflowState;

public sealed record ReservingState(
    FlightOption Selected,
    string ReservationId) : WorkflowState;

public sealed record AwaitingApprovalState(
    string ReservationId,
    FlightOption Selected,
    decimal TotalPrice,
    DateTimeOffset ReservationExpiry) : WorkflowState;

public sealed record ConfirmedState(string BookingRef) : WorkflowState;

public sealed record FailedState(
    string Reason,
    int AttemptCount) : WorkflowState;

public sealed record CancelledState(string Reason) : WorkflowState;

// ---------------------------------------------------------------------------
// Transition validation helpers
// ---------------------------------------------------------------------------

public static class WorkflowTransition
{
    /// <summary>
    /// Returns true when transitioning from <paramref name="from"/> to
    /// <paramref name="to"/> follows the allowed state machine path.
    /// </summary>
    public static bool IsValid(WorkflowState from, WorkflowState to) =>
        (from, to) switch
        {
            (IdleState,             SearchingState)        => true,
            (SearchingState,        ComparingState)        => true,
            (ComparingState,        ReservingState)        => true,
            (ReservingState,        AwaitingApprovalState) => true,
            (AwaitingApprovalState, ConfirmedState)        => true,
            (AwaitingApprovalState, CancelledState)        => true,
            (AwaitingApprovalState, FailedState)           => true,
            (FailedState,           ReservingState)        => true,  // user retry
            _ => false
        };
}
