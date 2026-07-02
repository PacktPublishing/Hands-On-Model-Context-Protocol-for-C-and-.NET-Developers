// Chapter 9 -- runnable adaptations of the agentic-workflow snippets.
//
// The verbatim ch09_*.cs files depend on IDistributedCache, ILogger<T>, and
// an MCP client instance; they are excluded from compilation. The classes
// below distil the same ideas (state machine, in-memory state store, guards,
// budget, resumer) into self-contained code that compiles against only the
// BCL and is exercised by Program.cs.

using System.Collections.Concurrent;
using System.Text.Json;

namespace TravelBooking.Chapter09;

// ---------------------------------------------------------------------------
// ch09_1 -- Workflow state hierarchy
// ---------------------------------------------------------------------------
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(IdleState),             nameof(IdleState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(SearchingState),        nameof(SearchingState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ComparingState),        nameof(ComparingState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ReservingState),        nameof(ReservingState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AwaitingApprovalState), nameof(AwaitingApprovalState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ConfirmedState),        nameof(ConfirmedState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(FailedState),           nameof(FailedState))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CancelledState),        nameof(CancelledState))]
public abstract record WorkflowState;
public sealed record IdleState                                : WorkflowState;
public sealed record SearchingState(string Origin, string Destination, DateOnly Date)
															  : WorkflowState;
public sealed record ComparingState(IReadOnlyList<FlightOption> Options)
															  : WorkflowState;
public sealed record ReservingState(FlightOption Selected)    : WorkflowState;
public sealed record AwaitingApprovalState(string ReservationId, DateTimeOffset ExpiresAt)
															  : WorkflowState;
public sealed record ConfirmedState(string BookingReference) : WorkflowState;
public sealed record FailedState(string Step, string Reason) : WorkflowState;
public sealed record CancelledState(string Reason)           : WorkflowState;

public sealed record FlightOption(string FlightId, string Airline, decimal Price);

// ---------------------------------------------------------------------------
// ch09_2 -- Travel plan
// ---------------------------------------------------------------------------
public sealed record TravelPlan(string Origin, string Destination, DateOnly Date,
								IReadOnlyList<string> Steps);

// ---------------------------------------------------------------------------
// ch09_8 -- Workflow state store (in-memory replacement for IDistributedCache)
// ---------------------------------------------------------------------------
public sealed class WorkflowStateStore
{
	private readonly ConcurrentDictionary<string, string> _store = new();

	public Task TransitionAsync(string workflowId, WorkflowState newState,
								CancellationToken ct = default)
	{
		_store[workflowId] = JsonSerializer.Serialize<WorkflowState>(newState);
		return Task.CompletedTask;
	}

	public Task<WorkflowState?> LoadAsync(string workflowId, CancellationToken ct = default)
	{
		if (!_store.TryGetValue(workflowId, out var json))
			return Task.FromResult<WorkflowState?>(null);
		return Task.FromResult(JsonSerializer.Deserialize<WorkflowState>(json));
	}
}

// ---------------------------------------------------------------------------
// ch09_11 -- Transition guard
// ---------------------------------------------------------------------------
public sealed class InvalidTransitionException(string message) : Exception(message);

public sealed class TransitionGuard
{
	private static readonly Dictionary<Type, string[]> Allowed = new()
	{
		[typeof(IdleState)]             = Array.Empty<string>(),
		[typeof(SearchingState)]        = new[] { "search_flights" },
		[typeof(ComparingState)]        = new[] { "search_flights" },
		[typeof(ReservingState)]        = new[] { "reserve_flight" },
		[typeof(AwaitingApprovalState)] = new[] { "book_flight" },
		[typeof(ConfirmedState)]        = new[] { "cancel_flight", "get_itinerary" },
		[typeof(FailedState)]           = Array.Empty<string>(),
		[typeof(CancelledState)]        = Array.Empty<string>(),
	};

	private static readonly HashSet<string> AlwaysAllowed =
		new(StringComparer.Ordinal) { "get_itinerary", "list_airports" };

	public void AssertAllowed(WorkflowState state, string toolName)
	{
		if (AlwaysAllowed.Contains(toolName)) return;
		if (!Allowed.TryGetValue(state.GetType(), out var tools)
			|| !tools.Contains(toolName, StringComparer.Ordinal))
		{
			throw new InvalidTransitionException(
				$"Tool '{toolName}' is not valid in state '{state.GetType().Name}'. " +
				$"Allowed: [{string.Join(", ", tools ?? Array.Empty<string>())}].");
		}
	}
}

// ---------------------------------------------------------------------------
// ch09_10 -- Workflow budget
// ---------------------------------------------------------------------------
public sealed class WorkflowBudgetExceededException(string message) : Exception(message);

public sealed class WorkflowBudget
{
	private readonly ConcurrentDictionary<string, int> _calls = new();
	public int MaxToolCalls { get; }

	public WorkflowBudget(int maxToolCalls = 10) => MaxToolCalls = maxToolCalls;

	public int Consume(string workflowId)
	{
		var n = _calls.AddOrUpdate(workflowId, 1, (_, prev) => prev + 1);
		if (n > MaxToolCalls)
			throw new WorkflowBudgetExceededException(
				$"Workflow '{workflowId}' exceeded budget of {MaxToolCalls} tool calls (used {n}).");
		return n;
	}

	public int Used(string workflowId) => _calls.GetValueOrDefault(workflowId);
	public void Complete(string workflowId) => _calls.TryRemove(workflowId, out _);
}

// ---------------------------------------------------------------------------
// ch09_12 -- Sustained error guard
// ---------------------------------------------------------------------------
public sealed class EmergencyStopException(string message) : Exception(message);

public sealed class SustainedErrorGuard
{
	private readonly ConcurrentDictionary<string, int> _failures = new();
	public int Threshold { get; }

	public SustainedErrorGuard(int threshold = 3) => Threshold = threshold;

	public void RecordFailure(string toolName)
	{
		var count = _failures.AddOrUpdate(toolName, 1, (_, prev) => prev + 1);
		if (count >= Threshold)
			throw new EmergencyStopException(
				$"Tool '{toolName}' failed {count} consecutive times. Halting workflow.");
	}

	public void RecordSuccess(string toolName) => _failures.TryRemove(toolName, out _);
	public int ConsecutiveFailures(string toolName) => _failures.GetValueOrDefault(toolName);
}

// ---------------------------------------------------------------------------
// ch09_3..ch09_7 -- Executor agent
// ---------------------------------------------------------------------------
public sealed record ExecutionResult(string WorkflowId, WorkflowState FinalState,
									 IReadOnlyList<string> Trace);

public sealed class TravelExecutorAgent
{
	private readonly WorkflowStateStore _store;
	private readonly TransitionGuard _guard;
	private readonly WorkflowBudget _budget;
	private readonly SustainedErrorGuard _errors;
	private readonly Func<string, IReadOnlyDictionary<string, object?>, CancellationToken, Task<object?>> _invoke;

	public TravelExecutorAgent(WorkflowStateStore store,
							   TransitionGuard guard,
							   WorkflowBudget budget,
							   SustainedErrorGuard errors,
							   Func<string, IReadOnlyDictionary<string, object?>, CancellationToken, Task<object?>> invoke)
	{
		_store = store; _guard = guard; _budget = budget;
		_errors = errors; _invoke = invoke;
	}

	public async Task<ExecutionResult> ExecuteAsync(
		string workflowId, TravelPlan plan, CancellationToken ct = default)
	{
		var trace = new List<string>();
		WorkflowState state = new IdleState();
		await _store.TransitionAsync(workflowId, state, ct);

		try
		{
			state = new SearchingState(plan.Origin, plan.Destination, plan.Date);
			await Move(workflowId, state, trace, ct);

			_guard.AssertAllowed(state, "search_flights");
			_budget.Consume(workflowId);
			var opts = (IReadOnlyList<FlightOption>)(await SafeInvoke("search_flights",
				new Dictionary<string, object?>
				{
					["origin"] = plan.Origin,
					["destination"] = plan.Destination,
					["date"] = plan.Date.ToString("yyyy-MM-dd"),
				}, ct))!;

			state = new ComparingState(opts);
			await Move(workflowId, state, trace, ct);

			var selected = opts.OrderBy(o => o.Price).First();
			state = new ReservingState(selected);
			await Move(workflowId, state, trace, ct);

			_guard.AssertAllowed(state, "reserve_flight");
			_budget.Consume(workflowId);
			var reservationId = (string)(await SafeInvoke("reserve_flight",
				new Dictionary<string, object?> { ["flight_id"] = selected.FlightId }, ct))!;

			state = new AwaitingApprovalState(reservationId, DateTimeOffset.UtcNow.AddMinutes(15));
			await Move(workflowId, state, trace, ct);

			_guard.AssertAllowed(state, "book_flight");
			_budget.Consume(workflowId);
			var bookingRef = (string)(await SafeInvoke("book_flight",
				new Dictionary<string, object?> { ["reservation_id"] = reservationId }, ct))!;

			state = new ConfirmedState(bookingRef);
			await Move(workflowId, state, trace, ct);
		}
		catch (EmergencyStopException)
		{
			state = new FailedState(state.GetType().Name, "emergency-stop");
			await Move(workflowId, state, trace, ct);
			throw;
		}
		catch (WorkflowBudgetExceededException)
		{
			state = new FailedState(state.GetType().Name, "budget-exceeded");
			await Move(workflowId, state, trace, ct);
			throw;
		}
		catch (Exception ex)
		{
			state = new FailedState(state.GetType().Name, ex.Message);
			await Move(workflowId, state, trace, ct);
		}
		finally
		{
			_budget.Complete(workflowId);
		}

		return new ExecutionResult(workflowId, state, trace);

		async Task<object?> SafeInvoke(string tool, IReadOnlyDictionary<string, object?> args, CancellationToken tok)
		{
			try
			{
				var result = await _invoke(tool, args, tok);
				_errors.RecordSuccess(tool);
				return result;
			}
			catch
			{
				_errors.RecordFailure(tool);
				throw;
			}
		}
	}

	private async Task Move(string workflowId, WorkflowState state,
							List<string> trace, CancellationToken ct)
	{
		trace.Add(state.GetType().Name);
		await _store.TransitionAsync(workflowId, state, ct);
	}
}

// ---------------------------------------------------------------------------
// ch09_9 -- Workflow resumer
// ---------------------------------------------------------------------------
public sealed class WorkflowNotFoundException(string id)
	: Exception($"Workflow '{id}' not found.");

public sealed record ResumeOutcome(string WorkflowId, string Action, WorkflowState State);

public sealed class WorkflowResumer
{
	private readonly WorkflowStateStore _store;

	public WorkflowResumer(WorkflowStateStore store) => _store = store;

	public async Task<ResumeOutcome> ResumeAsync(string workflowId, CancellationToken ct = default)
	{
		var state = await _store.LoadAsync(workflowId, ct)
					?? throw new WorkflowNotFoundException(workflowId);

		return state switch
		{
			ConfirmedState or CancelledState =>
				new ResumeOutcome(workflowId, "already-terminated", state),
			AwaitingApprovalState approval =>
				new ResumeOutcome(workflowId, $"re-prompt-approval (expires {approval.ExpiresAt:HH:mm:ssZ})", state),
			FailedState failed =>
				new ResumeOutcome(workflowId, $"retry-from-{failed.Step}", state),
			_ =>
				new ResumeOutcome(workflowId, "invalid-state", state),
		};
	}
}
