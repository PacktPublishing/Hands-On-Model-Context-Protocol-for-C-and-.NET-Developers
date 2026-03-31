// Chapter 5 — Section 5.1.2
// Environment-conditional DI registration for the idempotency store.
// Development uses an in-memory store; all other environments use Redis.
// Also registers scoped domain services consumed by FlightTools via constructor injection.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Domain services — scoped so each HTTP request gets its own instance
builder.Services.AddScoped<IFlightSearchService, FlightSearchService>();
builder.Services.AddScoped<IFlightBookingService, FlightBookingService>();

// IIdempotencyStore — swap implementation based on environment
if (builder.Environment.IsDevelopment())
{
    // No Redis required for local development
    builder.Services.AddScoped<IIdempotencyStore, InMemoryIdempotencyStore>();
}
else
{
    // Redis-backed store for staging and production
    builder.Services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
}

// Singleton cache for reference data (IATA airport codes, airline names)
builder.Services.AddMemoryCache();

// Hosted service that validates all tool attributes at startup
builder.Services.AddHostedService<CapabilityValidationService>();
