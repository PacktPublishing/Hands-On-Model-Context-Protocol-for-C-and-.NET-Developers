// Chapter 5 — Section 5.2.2
// Domain records for the FlightsMcpServer.
// C# records with positional constructors are preferred: immutable by default,
// structural equality built in, and System.Text.Json serialises them reliably.
// [Description] on properties flows through to the MCP JSON Schema.

using System.ComponentModel;

public record PassengerInput(
    [property: Description("Passenger first name as on passport")] string FirstName,
    [property: Description("Passenger last name as on passport")] string LastName,
    [property: Description("Machine-readable passport number")] string PassportNumber,
    [property: Description("Date of birth in ISO 8601 format (YYYY-MM-DD)")] string DateOfBirth);

public record FlightOption(
    string FlightId,
    string Airline,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    Money Price,
    int SeatsAvailable);

public record FlightSearchResult(
    IReadOnlyList<FlightOption> Options,
    string SearchId,
    DateTimeOffset ExpiresAt);

public record BookingConfirmation(
    string BookingReference,
    string FlightId,
    IReadOnlyList<string> PassengerNames,
    Money TotalPrice,
    DateTimeOffset BookedAt,
    string Status);

public record CancellationResult(
    string BookingReference,
    string Status,
    Money? RefundAmount,
    string Message);

public record Money(decimal Amount, string Currency);
