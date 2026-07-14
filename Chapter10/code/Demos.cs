// Chapter 10 -- runnable adaptations of the multi-agent coordination snippets.
//
// The verbatim ch10_*.cs files depend on IMcpClient / IMcpServer / ILogger<T>
// and are excluded from compilation. The types below distil the same ideas
// (agent contracts, specialist agents, coordinator, handoff context, conflict
// resolver, escalation handler) into self-contained code exercised by
// Program.cs.

using System.Collections.Concurrent;

namespace TravelBooking.Chapter10;

// ---------------------------------------------------------------------------
// ch10_1 -- Agent contracts
// ---------------------------------------------------------------------------
public sealed record AgentRequest(string SessionId, string Intent,
								  IReadOnlyDictionary<string, string> Parameters);

public sealed record AgentResponse(string AgentName, bool Success,
								   string Summary,
								   IReadOnlyDictionary<string, string> Data,
								   bool RequiresEscalation = false);

public interface IAgent
{
	string Name { get; }
	Task<AgentResponse> HandleAsync(AgentRequest request, HandoffContext context,
									CancellationToken ct = default);
}

// ---------------------------------------------------------------------------
// ch10_6 -- Handoff context (shared conversation state passed between agents)
// ---------------------------------------------------------------------------
public sealed class HandoffContext
{
	private readonly ConcurrentDictionary<string, string> _values = new();
	public string SessionId { get; }
	public List<string> Trail { get; } = new();

	public HandoffContext(string sessionId) => SessionId = sessionId;

	public void Set(string key, string value) => _values[key] = value;
	public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;
	public IReadOnlyDictionary<string, string> Snapshot() => _values.ToArray()
		.ToDictionary(kv => kv.Key, kv => kv.Value);
}

// ---------------------------------------------------------------------------
// ch10_2 -- Specialist agents (Flight, Hotel)
// ---------------------------------------------------------------------------
public sealed class FlightAgent : IAgent
{
	public string Name => "flight-agent";

	public Task<AgentResponse> HandleAsync(AgentRequest req, HandoffContext ctx, CancellationToken ct = default)
	{
		var origin = req.Parameters.GetValueOrDefault("origin",  "LHR");
		var dest   = req.Parameters.GetValueOrDefault("destination", "AMS");
		var price  = 389m;

		ctx.Set("selected.flight", $"{origin}->{dest} @ {price} USD");
		ctx.Trail.Add(Name);

		return Task.FromResult(new AgentResponse(
			Name, true,
			$"Chose BA010 {origin}->{dest} for {price:N0} USD",
			new Dictionary<string, string>
			{
				["flight_id"] = "BA010",
				["price"]     = price.ToString("0"),
			}));
	}
}

public sealed class HotelAgent : IAgent
{
	public string Name => "hotel-agent";

	public Task<AgentResponse> HandleAsync(AgentRequest req, HandoffContext ctx, CancellationToken ct = default)
	{
		var dest    = req.Parameters.GetValueOrDefault("destination", "AMS");
		var nights  = int.Parse(req.Parameters.GetValueOrDefault("nights", "3"));
		var perNight = 145m;
		var total   = nights * perNight;

		ctx.Set("selected.hotel", $"NH Grand {dest} x{nights} = {total} USD");
		ctx.Trail.Add(Name);

		return Task.FromResult(new AgentResponse(
			Name, true,
			$"Chose NH Grand {dest} for {nights} nights = {total:N0} USD",
			new Dictionary<string, string>
			{
				["hotel_id"] = "NH-AMS-01",
				["price"]    = total.ToString("0"),
			}));
	}
}

// ---------------------------------------------------------------------------
// ch10_3 -- Budget-checker agent (checks combined cost against a cap)
// ---------------------------------------------------------------------------
public sealed class BudgetCheckerAgent : IAgent
{
	private readonly decimal _capUsd;
	public string Name => "budget-agent";

	public BudgetCheckerAgent(decimal capUsd) => _capUsd = capUsd;

	public Task<AgentResponse> HandleAsync(AgentRequest req, HandoffContext ctx, CancellationToken ct = default)
	{
		var flightPrice = decimal.Parse(ctx.Get("flight.price") ?? "0");
		var hotelPrice  = decimal.Parse(ctx.Get("hotel.price")  ?? "0");
		var total       = flightPrice + hotelPrice;

		ctx.Trail.Add(Name);

		if (total > _capUsd)
		{
			return Task.FromResult(new AgentResponse(
				Name, false,
				$"Total {total:N0} USD exceeds budget cap {_capUsd:N0} USD -- escalate.",
				new Dictionary<string, string>
				{
					["total"] = total.ToString("0"),
					["cap"]   = _capUsd.ToString("0"),
				},
				RequiresEscalation: true));
		}

		return Task.FromResult(new AgentResponse(
			Name, true,
			$"Total {total:N0} USD within budget cap {_capUsd:N0} USD.",
			new Dictionary<string, string>
			{
				["total"] = total.ToString("0"),
				["cap"]   = _capUsd.ToString("0"),
			}));
	}
}

// ---------------------------------------------------------------------------
// ch10_4 -- Shared MCP resource (in-memory shared value store)
// ---------------------------------------------------------------------------
public sealed class SharedResource
{
	private readonly ConcurrentDictionary<string, object> _store = new();

	public T? Get<T>(string key) => _store.TryGetValue(key, out var v) ? (T)v : default;
	public void Set<T>(string key, T value) where T : notnull => _store[key] = value;
	public IReadOnlyDictionary<string, object> Snapshot() =>
		_store.ToArray().ToDictionary(kv => kv.Key, kv => kv.Value);
}

// ---------------------------------------------------------------------------
// ch10_7 -- Conflict resolver (picks the cheaper of two proposals)
// ---------------------------------------------------------------------------
public sealed record Proposal(string AgentName, string Description, decimal PriceUsd);

public sealed class ConflictResolver
{
	public Proposal Resolve(IReadOnlyList<Proposal> proposals)
	{
		if (proposals.Count == 0) throw new ArgumentException("No proposals to resolve.");
		return proposals.OrderBy(p => p.PriceUsd).First();
	}
}

// ---------------------------------------------------------------------------
// ch10_8 -- Escalation handler (records human-in-the-loop escalations)
// ---------------------------------------------------------------------------
public sealed record Escalation(string SessionId, string FromAgent, string Reason,
								DateTimeOffset At);

public sealed class EscalationHandler
{
	private readonly ConcurrentQueue<Escalation> _queue = new();
	public int Count => _queue.Count;

	public Escalation Raise(string sessionId, string fromAgent, string reason)
	{
		var e = new Escalation(sessionId, fromAgent, reason, DateTimeOffset.UtcNow);
		_queue.Enqueue(e);
		return e;
	}

	public IReadOnlyList<Escalation> Snapshot() => _queue.ToArray();
}

// ---------------------------------------------------------------------------
// ch10_5 -- Agent coordinator (routes a request through the specialists)
// ---------------------------------------------------------------------------
public sealed record CoordinatorResult(string SessionId, bool Success,
									   IReadOnlyList<AgentResponse> Responses,
									   Escalation? Escalation);

public sealed class AgentCoordinator
{
	private readonly FlightAgent _flight;
	private readonly HotelAgent _hotel;
	private readonly BudgetCheckerAgent _budget;
	private readonly EscalationHandler _escalations;

	public AgentCoordinator(FlightAgent flight, HotelAgent hotel,
							BudgetCheckerAgent budget,
							EscalationHandler escalations)
	{
		_flight = flight; _hotel = hotel; _budget = budget;
		_escalations = escalations;
	}

	public async Task<CoordinatorResult> HandleAsync(AgentRequest req, HandoffContext ctx,
													 CancellationToken ct = default)
	{
		var responses = new List<AgentResponse>();

		var flightRes = await _flight.HandleAsync(req, ctx, ct);
		responses.Add(flightRes);
		ctx.Set("flight.price", flightRes.Data.GetValueOrDefault("price", "0"));

		var hotelRes  = await _hotel.HandleAsync(req, ctx, ct);
		responses.Add(hotelRes);
		ctx.Set("hotel.price", hotelRes.Data.GetValueOrDefault("price", "0"));

		var budgetRes = await _budget.HandleAsync(req, ctx, ct);
		responses.Add(budgetRes);

		if (budgetRes.RequiresEscalation)
		{
			var esc = _escalations.Raise(req.SessionId, budgetRes.AgentName, budgetRes.Summary);
			return new CoordinatorResult(req.SessionId, false, responses, esc);
		}
		return new CoordinatorResult(req.SessionId, true, responses, null);
	}
}
