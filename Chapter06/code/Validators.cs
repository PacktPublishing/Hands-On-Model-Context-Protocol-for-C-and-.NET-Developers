// Chapter 6 — Section 6.4.2
// FluentValidation validators for the three FlightsServer tool input records.
// Each validator enforces business rules that JSON Schema cannot express:
// IATA format, future-date requirement, UUID key format, and reference patterns.

using FluentValidation;

namespace TravelBooking.Chapter06;

public sealed class SearchFlightsValidator : AbstractValidator<SearchFlightsInput>
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

        RuleFor(x => x)
            .Must(x => !string.Equals(x.Origin, x.Destination, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Origin and destination must be different airports.")
            .When(x => !string.IsNullOrEmpty(x.Origin) && !string.IsNullOrEmpty(x.Destination));

        RuleFor(x => x.DepartureDate)
            .Must(d => DateOnly.TryParse(d, out var date)
                       && date >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Departure date must be today or in the future.");

        RuleFor(x => x.PassengerCount)
            .InclusiveBetween(1, 9);
    }
}

public sealed class BookFlightValidator : AbstractValidator<BookFlightInput>
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

public sealed class CancelFlightValidator : AbstractValidator<CancelFlightInput>
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
