// Chapter 6 — Unit tests for the error-sanitisation filter (Section 6.4.4).
// Verifies that:
//   - McpException messages pass through verbatim (authored for the LLM host).
//   - Known domain exceptions yield a safe message + correlation ID.
//   - Unknown exceptions never leak Exception.Message to the client.
//   - The instance overload logs unexpected exceptions but stays silent for McpException.

using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using TravelBooking.Chapter06;
using Xunit;

namespace TravelBooking.Chapter06.Tests;

public class McpErrorSanitisationFilterTests
{
    private const string FixedId = "ABCDEF012345";

    private sealed class FixedCorrelationIds : ICorrelationIdProvider
    {
        public string NewId() => FixedId;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
    }

    [Fact]
    public void McpException_message_passes_through_verbatim()
    {
        var ex = new McpException("Origin and destination must be different airports.");

        var message = McpErrorSanitisationFilter.GetClientMessage(ex, FixedId);

        Assert.Equal("Origin and destination must be different airports.", message);
        Assert.DoesNotContain(FixedId, message);
    }

    [Fact]
    public void Domain_exception_produces_safe_message_with_correlation_id()
    {
        var ex = new FlightNotAvailableException("Flight FL-001 sold out at row 17 of bookings table");

        var message = McpErrorSanitisationFilter.GetClientMessage(ex, FixedId);

        Assert.Contains("not available", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(FixedId, message);
        // Internal detail must not leak.
        Assert.DoesNotContain("row 17", message);
        Assert.DoesNotContain("bookings table", message);
    }

    [Fact]
    public void Unknown_exception_yields_generic_message_with_correlation_id()
    {
        var ex = new InvalidOperationException("Connection string failed: Server=db-prod-01;Pwd=hunter2");

        var message = McpErrorSanitisationFilter.GetClientMessage(ex, FixedId);

        Assert.Contains("unexpected", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(FixedId, message);
        // Sensitive internal detail must never leak.
        Assert.DoesNotContain("hunter2", message);
        Assert.DoesNotContain("db-prod-01", message);
        Assert.DoesNotContain("Connection string", message);
    }

    [Fact]
    public void Instance_filter_logs_unexpected_exceptions()
    {
        var logger = new CapturingLogger<McpErrorSanitisationFilter>();
        var filter = new McpErrorSanitisationFilter(new FixedCorrelationIds(), logger);

        var message = filter.GetClientMessage(new InvalidOperationException("internal"));

        Assert.Contains(FixedId, message);
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        Assert.Contains(FixedId, logger.Entries[0].Message);
    }

    [Fact]
    public void Instance_filter_does_not_log_McpException()
    {
        var logger = new CapturingLogger<McpErrorSanitisationFilter>();
        var filter = new McpErrorSanitisationFilter(new FixedCorrelationIds(), logger);

        _ = filter.GetClientMessage(new McpException("deliberate client error"));

        Assert.Empty(logger.Entries);
    }
}

public class GuidCorrelationIdProviderTests
{
    [Fact]
    public void Generates_twelve_character_uppercase_hex_id()
    {
        var provider = new GuidCorrelationIdProvider();

        var id = provider.NewId();

        Assert.Equal(12, id.Length);
        Assert.Matches("^[0-9A-F]{12}$", id);
    }

    [Fact]
    public void Successive_ids_are_unique()
    {
        var provider = new GuidCorrelationIdProvider();
        var ids = Enumerable.Range(0, 100).Select(_ => provider.NewId()).ToHashSet();
        Assert.Equal(100, ids.Count);
    }
}

public class SanitisedToolInvokerTests
{
    [Fact]
    public async Task Successful_handler_result_passes_through()
    {
        var filter = new McpErrorSanitisationFilter(new GuidCorrelationIdProvider());
        var invoker = new SanitisedToolInvoker(filter);

        var result = await invoker.InvokeAsync(_ => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task McpException_from_handler_passes_message_through()
    {
        var filter = new McpErrorSanitisationFilter(new GuidCorrelationIdProvider());
        var invoker = new SanitisedToolInvoker(filter);

        var ex = await Assert.ThrowsAsync<McpException>(() => invoker.InvokeAsync<int>(
            _ => throw new McpException("validation: origin missing"),
            CancellationToken.None));

        Assert.Equal("validation: origin missing", ex.Message);
    }

    [Fact]
    public async Task Unknown_exception_is_replaced_with_sanitised_McpException()
    {
        var filter = new McpErrorSanitisationFilter(new GuidCorrelationIdProvider());
        var invoker = new SanitisedToolInvoker(filter);

        var ex = await Assert.ThrowsAsync<McpException>(() => invoker.InvokeAsync<int>(
            _ => throw new InvalidOperationException("Server=db-prod-01;Pwd=hunter2"),
            CancellationToken.None));

        Assert.Contains("unexpected", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hunter2", ex.Message);
    }
}
