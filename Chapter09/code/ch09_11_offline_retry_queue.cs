// Chapter 9 — Section 9.3.3
// SQLite-backed offline retry queue using EF Core.
// Buffered write operations replay in chronological submission order on reconnect.
// Causal ordering is critical: book_flight references a flightId from a prior search_flights result.
// After MaxAttempts failures the entry surfaces as a conflict for user review.

using Microsoft.EntityFrameworkCore;

namespace TravelBooking.Blazor.Services;

public sealed class OfflineCallEntry
{
    public int Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string SerializedArgs { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public int Attempts { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class OfflineQueueDbContext(DbContextOptions<OfflineQueueDbContext> opts)
    : DbContext(opts)
{
    public DbSet<OfflineCallEntry> OfflineCalls => Set<OfflineCallEntry>();

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<OfflineCallEntry>()
            .HasIndex(e => e.SubmittedAt);
}

public sealed class OfflineRetryQueue(
    OfflineQueueDbContext db,
    ModelContextProtocol.Client.McpClient inner,
    ILogger<OfflineRetryQueue> logger)
{
    private const int MaxAttempts = 3;

    public async Task EnqueueAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken ct = default)
    {
        db.OfflineCalls.Add(new OfflineCallEntry
        {
            ToolName = toolName,
            SerializedArgs = System.Text.Json.JsonSerializer.Serialize(args),
            SubmittedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        // OrderBy SubmittedAt preserves causal ordering of dependent calls.
        var pending = await db.OfflineCalls
            .Where(c => c.Attempts < MaxAttempts)
            .OrderBy(c => c.SubmittedAt)
            .ToListAsync(ct);

        foreach (var entry in pending)
        {
            try
            {
                var args = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, object?>>(entry.SerializedArgs)!;
                await inner.CallToolAsync(entry.ToolName, args, ct);
                db.OfflineCalls.Remove(entry);
                logger.LogInformation(
                    "Replayed offline call {Tool} from {Time}",
                    entry.ToolName, entry.SubmittedAt);
            }
            catch (Exception ex)
            {
                entry.Attempts++;
                entry.FailureReason = ex.Message;
                logger.LogWarning(
                    "Offline replay attempt {N}/{Max} failed for {Tool}: {Msg}",
                    entry.Attempts, MaxAttempts, entry.ToolName, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // Returns entries that have exhausted all retry attempts for user review.
    public Task<List<OfflineCallEntry>> GetConflictsAsync(CancellationToken ct = default) =>
        db.OfflineCalls
            .Where(c => c.Attempts >= MaxAttempts)
            .OrderBy(c => c.SubmittedAt)
            .ToListAsync(ct);
}
