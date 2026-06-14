// Chapter 11 — runnable adaptations of the chapter snippets.
//
// CapabilityLibrary (ch11_2): registry of versioned capabilities that an
//   orchestrator can discover and resolve by name + minimum version.
// DeploymentSlot + RollbackController (ch11_6): blue/green slot model that
//   automatically rolls back when post-deploy health checks fail.

using System.Collections.Concurrent;

namespace TravelBooking.Chapter11;

// ---------------------------------------------------------------------------
// ch11_2 — Capability library
// ---------------------------------------------------------------------------
public sealed record Capability(string Name, Version Version, string Endpoint);

public sealed class CapabilityLibrary
{
    private readonly ConcurrentDictionary<string, List<Capability>> _byName = new();

    public void Register(Capability capability)
    {
        _byName.AddOrUpdate(capability.Name,
            _ => new List<Capability> { capability },
            (_, list) =>
            {
                lock (list) { list.Add(capability); list.Sort((a, b) => b.Version.CompareTo(a.Version)); }
                return list;
            });
    }

    public Capability? Resolve(string name, Version minimumVersion)
    {
        if (!_byName.TryGetValue(name, out var list)) return null;
        lock (list) { return list.FirstOrDefault(c => c.Version >= minimumVersion); }
    }

    public IReadOnlyList<Capability> ListAll()
        => _byName.Values.SelectMany(l => { lock (l) return l.ToArray(); }).ToArray();
}

// ---------------------------------------------------------------------------
// ch11_6 — Automated rollback
// ---------------------------------------------------------------------------
public sealed record DeploymentSlot(string Name, string Version);

public sealed class RollbackController
{
    private DeploymentSlot _active;
    private DeploymentSlot? _previous;
    public IReadOnlyList<string> Events => _events;
    private readonly List<string> _events = new();

    public RollbackController(DeploymentSlot initial) => _active = initial;

    public DeploymentSlot Active => _active;
    public DeploymentSlot? Previous => _previous;

    public async Task<DeploymentSlot> DeployAsync(
        DeploymentSlot candidate,
        Func<DeploymentSlot, CancellationToken, Task<bool>> healthCheck,
        CancellationToken ct = default)
    {
        _events.Add($"deploy:start {candidate.Name}@{candidate.Version}");
        var healthy = await healthCheck(candidate, ct).ConfigureAwait(false);
        if (!healthy)
        {
            _events.Add($"deploy:fail {candidate.Name}@{candidate.Version}; keep {_active.Version}");
            return _active;
        }

        _previous = _active;
        _active = candidate;
        _events.Add($"deploy:promote {candidate.Version} (rollback target={_previous.Version})");
        return _active;
    }

    public DeploymentSlot Rollback()
    {
        if (_previous is null)
            throw new InvalidOperationException("No previous slot to rollback to.");
        _events.Add($"rollback {_active.Version} -> {_previous.Version}");
        (_active, _previous) = (_previous, _active);
        return _active;
    }
}
