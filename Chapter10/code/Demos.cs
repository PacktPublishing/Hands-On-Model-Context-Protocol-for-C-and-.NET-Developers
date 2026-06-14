// Chapter 10 — runnable adaptations of the chapter snippets.
//
// CorrelationIdGenerator (ch10_7): produces deterministic, unique correlation
//   IDs and shows how a middleware would propagate one through a call chain.
// TenantIsolationGuard (ch10_3): rejects cross-tenant resource access by
//   matching the caller's tenant claim against the resource owner.

using System.Collections.Concurrent;

namespace TravelBooking.Chapter10;

// ---------------------------------------------------------------------------
// ch10_7 — Correlation IDs
// ---------------------------------------------------------------------------
public interface ICorrelationIdGenerator
{
    string New();
}

public sealed class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public string New() => Guid.NewGuid().ToString("N");
}

public sealed class CorrelationIdMiddlewareDemo
{
    private readonly ICorrelationIdGenerator _gen;

    public CorrelationIdMiddlewareDemo(ICorrelationIdGenerator gen) => _gen = gen;

    public async Task<(string Correlation, string Result)> InvokeAsync(
        IDictionary<string, string> headers,
        Func<string, CancellationToken, Task<string>> next,
        CancellationToken ct = default)
    {
        if (!headers.TryGetValue("X-Correlation-Id", out var id) || string.IsNullOrWhiteSpace(id))
        {
            id = _gen.New();
            headers["X-Correlation-Id"] = id;
        }
        var result = await next(id, ct).ConfigureAwait(false);
        return (id, result);
    }
}

// ---------------------------------------------------------------------------
// ch10_3 — Tenant isolation
// ---------------------------------------------------------------------------
public sealed record TenantResource(string ResourceId, string TenantId);

public sealed class CrossTenantAccessException : Exception
{
    public CrossTenantAccessException(string callerTenant, string resourceTenant)
        : base($"Caller tenant '{callerTenant}' cannot access resource owned by '{resourceTenant}'.")
    {
        CallerTenant = callerTenant;
        ResourceTenant = resourceTenant;
    }

    public string CallerTenant { get; }
    public string ResourceTenant { get; }
}

public sealed class TenantIsolationGuard
{
    private readonly ConcurrentDictionary<string, TenantResource> _store = new();

    public void Register(TenantResource resource) => _store[resource.ResourceId] = resource;

    public TenantResource Read(string callerTenantId, string resourceId)
    {
        if (!_store.TryGetValue(resourceId, out var resource))
            throw new KeyNotFoundException($"Resource '{resourceId}' not found.");

        if (!string.Equals(resource.TenantId, callerTenantId, StringComparison.Ordinal))
            throw new CrossTenantAccessException(callerTenantId, resource.TenantId);

        return resource;
    }
}
