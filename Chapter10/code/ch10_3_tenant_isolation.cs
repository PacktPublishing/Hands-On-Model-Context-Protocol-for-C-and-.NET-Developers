// Chapter 10 — Section 10.2.4
// Tenant isolation using EF Core global query filters.
// TenantContext extracts the tenant ID from the JWT tid claim.
// Global query filters on Booking and Itinerary ensure every query
// is automatically scoped to the current tenant — no per-handler filter required.
// TenantStampingInterceptor sets TenantId on every new entity before insert.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TravelBooking.Data;

public sealed class TenantContext(IHttpContextAccessor httpContext)
{
    // Throws fail-closed if the token carries no tid claim.
    public string TenantId =>
        httpContext.HttpContext?.User.FindFirstValue("tid")
        ?? throw new UnauthorizedAccessException(
            "No tenant claim in token. Request is not authorized.");
}

public sealed class BookingDbContext(
    DbContextOptions<BookingDbContext> options,
    TenantContext tenant) : DbContext(options)
{
    public DbSet<Booking>   Bookings   => Set<Booking>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Global query filters: every SELECT automatically adds WHERE TenantId = @tid.
        model.Entity<Booking>()
             .HasQueryFilter(b => b.TenantId == tenant.TenantId);
        model.Entity<Itinerary>()
             .HasQueryFilter(i => i.TenantId == tenant.TenantId);
    }
}

// Automatically stamps TenantId on every new entity before EF Core inserts it.
// Register as: services.AddScoped<ISaveChangesInterceptor, TenantStampingInterceptor>();
public sealed class TenantStampingInterceptor(TenantContext tenant)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity is ITenantEntity entity)
                entity.TenantId = tenant.TenantId;
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

public interface ITenantEntity
{
    string TenantId { get; set; }
}

public sealed class Booking : ITenantEntity
{
    public int    Id       { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string FlightId { get; set; } = string.Empty;
    public string Passenger { get; set; } = string.Empty;
}

public sealed class Itinerary : ITenantEntity
{
    public int    Id       { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
}
