// Chapter 1 — Adopt MCP on .NET: Problems, Patterns, and Payoff
// This project demonstrates the before/after contrast for MCP integration.
//
// ch01_1_without_mcp_integration.cs  — manual HTTP integration (pre-MCP)
// ch01_2_with_mcp_search_flights.cs  — SearchFlightsTool declared with MCP
//
// This program demonstrates both approaches with working examples.

using TravelBooking.CodeSamples;
using TravelBooking.CodeSamples.Shared;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Chapter 1 — Adopt MCP on .NET: Problems, Patterns, Payoff   ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Example search parameters
var origin = "LHR";
var destination = "JFK";
var date = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

Console.WriteLine($"Searching flights: {origin} → {destination} on {date:yyyy-MM-dd}");
Console.WriteLine();

// ============================================================================
// Approach 1: WITHOUT MCP (Pre-MCP manual integration)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("Approach 1: PRE-MCP Manual HTTP Integration");
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("Issues:");
Console.WriteLine("  • Tight coupling to HTTP implementation");
Console.WriteLine("  • Bespoke authentication per service");
Console.WriteLine("  • Brittle contracts - URL changes break all consumers");
Console.WriteLine("  • Each consumer must duplicate integration logic");
Console.WriteLine();

// Note: This would normally make HTTP calls, but for demo purposes
// we'll use a mock service to show the concept
var mockService = new MockFlightSearchService();
var result = await mockService.SearchAsync(origin, destination, date);

Console.WriteLine($"Found {result.TotalResults} flights (using mock service):");
foreach (var flight in result.Flights)
{
    Console.WriteLine($"  • {flight.Airline} {flight.FlightNumber}");
    Console.WriteLine($"    Departs: {flight.DepartureTime:HH:mm} → Arrives: {flight.ArrivalTime:HH:mm}");
    Console.WriteLine($"    Price: {flight.Price.Amount:C} {flight.Price.CurrencyCode} | Seats: {flight.SeatsAvailable}");
    Console.WriteLine();
}

// ============================================================================
// Approach 2: WITH MCP (Modern MCP integration)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("Approach 2: WITH MCP - Standardized Integration");
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("Benefits:");
Console.WriteLine("  ✓ Schema generated automatically from C# types");
Console.WriteLine("  ✓ Discoverable by any MCP-compliant host");
Console.WriteLine("  ✓ Authentication handled by transport layer");
Console.WriteLine("  ✓ Single declaration, consumed by any client (Blazor, LLM, CLI)");
Console.WriteLine("  ✓ Type-safe with Description attributes for documentation");
Console.WriteLine();

// The MCP tool (SearchFlightsTool) would be registered with the MCP server
// and discovered automatically by any MCP client
var mcpTool = new SearchFlightsTool(mockService);
var mcpResult = await mcpTool.SearchFlightsAsync(origin, destination, date.ToString("O"), CancellationToken.None);

Console.WriteLine($"MCP Tool Result: {mcpResult.TotalResults} flights discovered");
foreach (var flight in mcpResult.Flights.Take(2))
{
    Console.WriteLine($"  ✓ {flight.Airline} {flight.FlightNumber} @ {flight.Price.Amount:C}");
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("Key Takeaway: MCP standardizes capability exposure across .NET");
Console.WriteLine("See ch01_1_without_mcp_integration.cs vs ch01_2_with_mcp_search_flights.cs");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
