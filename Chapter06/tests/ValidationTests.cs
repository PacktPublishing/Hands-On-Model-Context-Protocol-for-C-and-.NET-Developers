// Chapter 6 — Unit tests for the FluentValidation validators (Section 6.4.2).
// These tests run with no external process — they exercise the rule logic directly.

using FluentValidation.Results;
using TravelBooking.Chapter06;
using Xunit;

namespace TravelBooking.Chapter06.Tests;

public class SearchFlightsValidatorTests
{
    private readonly SearchFlightsValidator _validator = new();

    private Task<ValidationResult> Validate(SearchFlightsInput input)
        => _validator.ValidateAsync(input, TestContext.Current.CancellationToken);

    private static SearchFlightsInput Valid(
        string origin = "LHR",
        string destination = "AMS",
        string? departureDate = null,
        int passengerCount = 1)
        => new(origin, destination, departureDate ?? DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"), passengerCount);

    [Fact]
    public async Task Valid_input_passes()
    {
        var result = await Validate(Valid());
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("LH")]
    [InlineData("LHRA")]
    [InlineData("lhr")]
    [InlineData("L1R")]
    [InlineData("")]
    public async Task Origin_must_be_three_uppercase_letters(string badOrigin)
    {
        var result = await Validate(Valid(origin: badOrigin));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchFlightsInput.Origin));
    }

    [Theory]
    [InlineData("am")]
    [InlineData("AMSX")]
    [InlineData("ams")]
    public async Task Destination_must_be_three_uppercase_letters(string badDestination)
    {
        var result = await Validate(Valid(destination: badDestination));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchFlightsInput.Destination));
    }

    [Fact]
    public async Task Origin_and_destination_must_differ()
    {
        var result = await Validate(Valid(origin: "LHR", destination: "LHR"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("different", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Past_departure_date_is_rejected()
    {
        var result = await Validate(Valid(departureDate: "2020-01-01"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Today_is_accepted_as_departure_date()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await Validate(Valid(departureDate: today));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Unparseable_departure_date_is_rejected()
    {
        var result = await Validate(Valid(departureDate: "not-a-date"));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public async Task Passenger_count_must_be_between_one_and_nine(int badCount)
    {
        var result = await Validate(Valid(passengerCount: badCount));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchFlightsInput.PassengerCount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task Passenger_count_in_range_is_accepted(int goodCount)
    {
        var result = await Validate(Valid(passengerCount: goodCount));
        Assert.True(result.IsValid);
    }
}

public class BookFlightValidatorTests
{
    private readonly BookFlightValidator _validator = new();

    private Task<ValidationResult> Validate(BookFlightInput input)
        => _validator.ValidateAsync(input, TestContext.Current.CancellationToken);

    private static BookFlightInput Valid()
        => new("FL-001", Guid.NewGuid().ToString(), "Ada Lovelace", "P1234567");

    [Fact]
    public async Task Valid_input_passes()
    {
        var result = await Validate(Valid());
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_flight_id_is_rejected()
    {
        var result = await Validate(Valid() with { FlightId = "" });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    public async Task Idempotency_key_must_be_a_uuid(string badKey)
    {
        var result = await Validate(Valid() with { IdempotencyKey = badKey });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BookFlightInput.IdempotencyKey));
    }

    [Fact]
    public async Task Empty_passenger_name_is_rejected()
    {
        var result = await Validate(Valid() with { PassengerName = "" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Passenger_name_over_max_length_is_rejected()
    {
        var result = await Validate(Valid() with { PassengerName = new string('x', 101) });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Empty_passport_number_is_rejected()
    {
        var result = await Validate(Valid() with { PassportNumber = "" });
        Assert.False(result.IsValid);
    }
}

public class CancelFlightValidatorTests
{
    private readonly CancelFlightValidator _validator = new();

    private Task<ValidationResult> Validate(CancelFlightInput input)
        => _validator.ValidateAsync(input, TestContext.Current.CancellationToken);

    private static CancelFlightInput Valid() => new("B-20250615-001", "Schedule change");

    [Fact]
    public async Task Valid_input_passes()
    {
        var result = await Validate(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("B-2025-001")]
    [InlineData("B-20250615-1")]
    [InlineData("X-20250615-001")]
    public async Task Booking_reference_must_match_pattern(string badReference)
    {
        var result = await Validate(Valid() with { BookingReference = badReference });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CancelFlightInput.BookingReference));
    }

    [Fact]
    public async Task Empty_reason_is_rejected()
    {
        var result = await Validate(Valid() with { Reason = "" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Reason_over_max_length_is_rejected()
    {
        var result = await Validate(Valid() with { Reason = new string('x', 501) });
        Assert.False(result.IsValid);
    }
}
