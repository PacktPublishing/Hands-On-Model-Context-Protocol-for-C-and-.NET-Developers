// Shared types for Chapter 02 examples
// These types extend Chapter 01 with booking, itinerary, and additional service interfaces

using Microsoft.Extensions.AI;

namespace TravelBooking.CodeSamples.Shared;

// ============================================================================
// Domain Models (from Chapter 01)
// ============================================================================

/// <summary>
/// Represents a flight option returned from search
/// </summary>
public record FlightOption(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    Money Price,
    int SeatsAvailable);

/// <summary>
/// Represents a monetary value with currency
/// </summary>
public record Money(decimal Amount, string CurrencyCode);

/// <summary>
/// Result of a flight search operation
/// </summary>
public record FlightSearchResult(List<FlightOption> Flights, int TotalResults);

// ============================================================================
// Booking Models (Chapter 02)
// ============================================================================

/// <summary>
/// Input data for a passenger booking
/// </summary>
public record PassengerInput(
    string FirstName,
    string LastName,
    string PassportNumber,
    string Email);

/// <summary>
/// Booking confirmation returned after successful booking
/// </summary>
public record BookingConfirmation(
    string BookingReference,
    string FlightId,
    List<string> PassengerNames,
    string TotalPrice,
    string Status);

/// <summary>
/// Detailed itinerary information
/// </summary>
public record ItineraryDetails(
    string BookingReference,
    string FlightId,
    string Origin,
    string Destination,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    List<string> PassengerNames,
    Money TotalPrice,
    string Status);

/// <summary>
/// Prompt message for MCP prompts (compatible with Microsoft.Extensions.AI)
/// </summary>
public record PromptMessage(ChatRole Role, string Content);

// ============================================================================
// Exceptions
// ============================================================================

/// <summary>
/// Exception thrown when a requested flight is no longer available
/// </summary>
public class FlightNotAvailableException : Exception
{
    public string FlightId { get; }
    
    public FlightNotAvailableException(string flightId, string message) 
        : base(message)
    {
        FlightId = flightId;
    }
}

// ============================================================================
// Service Interfaces
// ============================================================================

/// <summary>
/// Service interface for flight search operations
/// </summary>
public interface IFlightSearchService
{
    Task<FlightSearchResult> SearchAsync(
        string origin,
        string destination,
        DateOnly date,
        CancellationToken ct = default);
}

/// <summary>
/// Service interface for flight booking operations
/// </summary>
public interface IFlightBookingService
{
    Task<BookingConfirmation> BookAsync(
        string flightId,
        List<PassengerInput> passengers,
        CancellationToken ct = default);
}

/// <summary>
/// Service interface for itinerary retrieval
/// </summary>
public interface IItineraryService
{
    Task<ItineraryDetails?> GetAsync(
        string bookingReference,
        CancellationToken ct = default);
}
