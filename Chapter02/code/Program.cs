// Chapter 2 — MCP Fundamentals: Protocol, Roles, and Capabilities
// This project demonstrates MCP server tool, resource, and prompt implementations.

using TravelBooking.CodeSamples;
using TravelBooking.CodeSamples.Shared;
using TravelBooking.Demos;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       Chapter 2 — MCP Fundamentals: Tools & Capabilities       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Initialize mock services
var flightSearch = new MockFlightSearchService();
var flightBooking = new MockFlightBookingService();
var itineraryService = new MockItineraryService();

// ============================================================================
// Example 1: Contract Verification (ch02_1_search_flights_contract_tests.cs)
// ============================================================================
await ContractVerificationDemo.RunAsync();

// ============================================================================
// Example 2: Book Flight Tool (ch02_2_book_flight_tool.cs)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("Example 2: BookFlightTool - Business Logic & Error Handling");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

var bookFlightTool = new BookFlightTool(flightBooking);

// Successful booking
Console.WriteLine("\n✓ Successful Booking:");
var bookingResult = await bookFlightTool.BookFlightAsync(
    "FL-LHR-JFK-001",
    "John",
    "Doe",
    "P123456789",
    CancellationToken.None);
Console.WriteLine($"  {bookingResult}");

// Failed booking (empty flightId)
Console.WriteLine("\n✗ Failed Booking (validation error):");
var failedBooking = await bookFlightTool.BookFlightAsync(
    "",
    "Jane",
    "Smith",
    "P987654321",
    CancellationToken.None);
Console.WriteLine($"  {failedBooking}");

// ============================================================================
// Example 3: Itinerary Resource Handler (ch02_3_itinerary_resource_handler.cs)
// ============================================================================
Console.WriteLine("\n─────────────────────────────────────────────────────────────────");
Console.WriteLine("Example 3: Itinerary Resource Handler - Dynamic Resources");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

var itineraryResource = new ItineraryResourceHandler(itineraryService);

Console.WriteLine("\n✓ Retrieving itinerary for booking reference BK-SAMPLE123:");
var itineraryJson = await itineraryResource.GetItineraryAsync(
    "BK-SAMPLE123",
    CancellationToken.None);
Console.WriteLine($"  {itineraryJson[..100]}..."); // Show first 100 chars

// ============================================================================
// Example 4: Itinerary Summary Prompt (ch02_4_itinerary_summary_prompt.cs)
// ============================================================================
Console.WriteLine("\n─────────────────────────────────────────────────────────────────");
Console.WriteLine("Example 4: Itinerary Summary Prompt - Multi-Message Prompts");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

var summaryPrompt = new ItinerarySummaryPrompt(itineraryService);

Console.WriteLine("\n✓ Generating prompt for LLM to summarize itinerary:");
var messages = await summaryPrompt.ItinerarySummaryAsync(
    "BK-SAMPLE123",
    CancellationToken.None);

foreach (var message in messages)
{
    Console.WriteLine($"\n  [{message.Role}]");
    var content = message.Text?.Length > 80 
        ? message.Text[..80] + "..." 
        : message.Text;
    Console.WriteLine($"  {content}");
}

// ============================================================================
// Example 5: SearchFlights Tool Deprecation (ch02_5_search_flights_deprecation.cs)
// ============================================================================
Console.WriteLine("\n─────────────────────────────────────────────────────────────────");
Console.WriteLine("Example 5: Tool Versioning - Backward Compatibility");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

var searchTools = new SearchFlightsWithDeprecation(flightSearch);

var origin = "LHR";
var destination = "JFK";
var date = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

Console.WriteLine($"\n✓ V1 Tool (deprecated) - Searching {origin} → {destination}:");
var v1Result = await searchTools.SearchFlights_v1Async(
    origin,
    destination,
    date.ToString("O"),
    CancellationToken.None);
Console.WriteLine($"  Found {v1Result.TotalResults} flights");

Console.WriteLine($"\n✓ V2 Tool (current) - Same search with improved API:");
var v2Result = await searchTools.SearchFlightsAsync(
    origin,
    destination,
    date.ToString("O"),
    CancellationToken.None);
Console.WriteLine($"  Found {v2Result.TotalResults} flights with enhanced data");

Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
Console.WriteLine("Chapter 2 demonstrations completed successfully!");
Console.WriteLine();
Console.WriteLine("📝 Note: Examples 1 & 6 (contract/schema tests) are shown as demos.");
Console.WriteLine("   See CONTRACT_TESTS_README.md for running actual integration tests.");
Console.WriteLine();
Console.WriteLine("Key Concepts: Tools, Resources, Prompts, Versioning, Contracts");
Console.WriteLine("═══════════════════════════════════════════════════════════════");


