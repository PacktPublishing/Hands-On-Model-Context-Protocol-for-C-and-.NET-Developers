// Chapter 5 — Section 5.2.2
// PassengerInput with data annotation constraints.
// [StringLength] and [Range] attributes tighten the JSON Schema that the SDK
// generates and exposes to LLM clients, reducing invalid inputs at the source.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public record PassengerInput(
    [property: Description("Passenger first name as on passport")]
    [property: StringLength(50, MinimumLength = 1)]
    string FirstName,

    [property: Description("Passenger last name as on passport")]
    [property: StringLength(80, MinimumLength = 1)]
    string LastName,

    [property: Description("Machine-readable passport number (6-9 alphanumeric characters)")]
    [property: StringLength(9, MinimumLength = 6)]
    string PassportNumber,

    [property: Description("Date of birth in ISO 8601 format (YYYY-MM-DD)")]
    string DateOfBirth);

// Separate record for the search request to apply range constraints
public record FlightSearchRequest(
    [property: Description("IATA origin airport code (e.g. LHR)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Origin,

    [property: Description("IATA destination airport code (e.g. JFK)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Destination,

    [property: Description("Departure date in ISO 8601 format (YYYY-MM-DD)")]
    string DepartureDate,

    [property: Description("Number of passengers")]
    [property: Range(1, 9)]
    int PassengerCount = 1);
