// Chapter 6 — Section 6.4.2
// FluentValidation validators for all three FlightsServer tool input types.
// Each validator enforces business rules that JSON Schema cannot express:
// IATA format, future-date requirement, UUID key format, and reference patterns.
// Call ValidateAsync at the top of each handler and throw McpException on failure.
// McpException messages reach the LLM host; write them to be actionable, not internal.

using FluentValidation;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.FlightsServer.Validation;

// Input records for the validated handler pattern.
// [property: StringLength] and [property: Range] cause the SDK schema generator
// to emit maxLength, minLength, minimum, and maximum constraints automatically.

public record SearchFlightsInput(
    [property: Description("IATA origin airport code (e.g. LHR)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Origin,

    [property: Description("IATA destination airport code (e.g. AMS)")]
    [property: StringLength(3, MinimumLength = 3)]
    string Destination,

    [property: Description("Departure date in ISO 8601 format (YYYY-MM-DD)")]
    string DepartureDate,

    [property: Description("Number of passengers (1-9)")]
    [property: Range(1, 9)]
    int PassengerCount = 1);

public record BookFlightInput(
    [property: Description("Flight identifier returned by search_flights")]
    string FlightId,

    [property: Description("Caller-supplied idempotency key (UUID v4)")]
    string IdempotencyKey,

    [property: Description("Passenger full name")]
    [property: StringLength(100, MinimumLength = 2)]
    string PassengerName,

    [property: Description("Passenger passport number")]
    string PassportNumber);

public record CancelFlightInput(
    [property: Description("Booking reference returned by book_flight")]
    string BookingReference,

    [property: Description("Reason for cancellation")]
    [property: StringLength(500)]
    string Reason);

// Validators — enforce rules that JSON Schema cannot express.

public class SearchFlightsValidator : AbstractValidator<SearchFlightsInput>
{
    public SearchFlightsValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Origin must be a 3-letter IATA code in uppercase.");

        RuleFor(x => x.Destination)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Destination must be a 3-letter IATA code in uppercase.");

        RuleFor(x => x.DepartureDate)
            .Must(d => DateOnly.TryParse(d, out var date)
                       && date >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Departure date must be today or in the future.");

        RuleFor(x => x.PassengerCount)
            .InclusiveBetween(1, 9);
    }
}

public class BookFlightValidator : AbstractValidator<BookFlightInput>
{
    public BookFlightValidator()
    {
        RuleFor(x => x.FlightId).NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .Must(k => Guid.TryParse(k, out _))
            .WithMessage("Idempotency key must be a valid UUID (e.g. 550e8400-e29b-41d4-a716-446655440000).");

        RuleFor(x => x.PassengerName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.PassportNumber).NotEmpty();
    }
}

public class CancelFlightValidator : AbstractValidator<CancelFlightInput>
{
    public CancelFlightValidator()
    {
        RuleFor(x => x.BookingReference)
            .NotEmpty()
            .Matches("^B-[0-9]{8}-[0-9]{3}$")
            .WithMessage("Booking reference must be in format B-YYYYMMDD-NNN.");

        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

// Handler integration — shows the standard validation call pattern.
// Replace FlightTools.SearchFlights in the Chapter 5 server with this validated version.
[McpServerToolType]
public sealed class ValidatedFlightTools
{
    private readonly IFlightSearchService _searchService;
    private readonly IFlightBookingService _bookingService;

    public ValidatedFlightTools(
        IFlightSearchService searchService,
        IFlightBookingService bookingService)
    {
        _searchService = searchService;
        _bookingService = bookingService;
    }

    [McpServerTool, Description("Search for available flights between two airports on a given date.")]
    public async Task<FlightSearchResult> SearchFlights(
        SearchFlightsInput input, CancellationToken cancellationToken = default)
    {
        var validation = await new SearchFlightsValidator().ValidateAsync(
            input, cancellationToken);
        if (!validation.IsValid)
            throw new McpException(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        return await _searchService.SearchAsync(
            input.Origin, input.Destination, input.DepartureDate,
            input.PassengerCount, cancellationToken);
    }

    [McpServerTool, Description("Cancel an existing flight booking and request a refund.")]
    public async Task<CancellationResult> CancelFlight(
        CancelFlightInput input, CancellationToken cancellationToken = default)
    {
        var validation = await new CancelFlightValidator().ValidateAsync(
            input, cancellationToken);
        if (!validation.IsValid)
            throw new McpException(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        return await _bookingService.CancelAsync(
            input.BookingReference, input.Reason, cancellationToken);
    }
}
