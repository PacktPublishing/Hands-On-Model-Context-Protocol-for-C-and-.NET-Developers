// Chapter 6 — Section 6.2.2
// BenchmarkDotNet micro-benchmarks for the SearchFlights hot path.
// [MemoryDiagnoser] adds Gen0/Gen1/Gen2 and Allocated columns to the output table.
// [Benchmark(Baseline = true)] marks SearchFlights_Baseline as the reference point;
// all other benchmarks report a Ratio column relative to it.
// Run in Release mode: dotnet run --project benchmarks/FlightsServer.Benchmarks -c Release

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class SearchFlightsBenchmarks
{
    private IFlightSearchService _service = null!;
    private CachedFlightSearchService _cachedService = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new StubFlightSearchService();
        _cachedService = new CachedFlightSearchService(_service);
    }

    [Benchmark(Baseline = true)]
    public async Task<FlightSearchResult> SearchFlights_Baseline()
        => await _service.SearchAsync("LHR", "AMS", "2025-06-15", 1,
                                      CancellationToken.None);

    [Benchmark]
    public async Task<FlightSearchResult> SearchFlights_WithResultCache()
        => await _cachedService.SearchWithCacheAsync("LHR", "AMS", "2025-06-15", 1,
                                                      CancellationToken.None);

    [Benchmark]
    public async Task<FlightSearchResult> SearchFlights_DifferentRoutes()
    {
        // Benchmarks cache-miss overhead by varying the route on each iteration.
        var suffix = Environment.TickCount % 100;
        return await _service.SearchAsync("LHR", "AMS", $"2025-07-{15 + (suffix % 10):D2}", 1,
                                          CancellationToken.None);
    }
}

// Cache decorator that demonstrates the allocation reduction visible in the benchmark output.
// The Dictionary<string, FlightSearchResult> avoids repeated async state machine allocations
// for repeated searches on the same route, reducing per-call allocated bytes.
public sealed class CachedFlightSearchService
{
    private readonly IFlightSearchService _inner;
    private readonly Dictionary<string, FlightSearchResult> _cache = new();

    public CachedFlightSearchService(IFlightSearchService inner)
        => _inner = inner;

    public async Task<FlightSearchResult> SearchWithCacheAsync(
        string origin, string destination, string departureDate,
        int passengerCount, CancellationToken cancellationToken)
    {
        var key = $"{origin}:{destination}:{departureDate}:{passengerCount}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var result = await _inner.SearchAsync(
            origin, destination, departureDate, passengerCount, cancellationToken);
        _cache[key] = result;
        return result;
    }
}

// Entry point for the benchmark project.
// The companion repo's benchmarks/ project calls BenchmarkRunner.Run<SearchFlightsBenchmarks>().
public static class BenchmarkEntryPoint
{
    public static void Run(string[] args) =>
        BenchmarkRunner.Run<SearchFlightsBenchmarks>();
}
