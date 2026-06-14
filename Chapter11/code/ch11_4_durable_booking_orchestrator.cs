// Chapter 11 — Section 11.3.3
// Durable Functions orchestrator for a multi-step booking workflow.
// Each CallActivityAsync call is a checkpoint: if the host restarts between steps,
// the framework replays history to restore state without re-executing completed activities.
// nameof() binds activity names at compile time, eliminating silent string mismatch bugs.

using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

public sealed class BookingOrchestration
{
    [Function(nameof(BookItinerary))]
    public async Task<BookingResult> BookItinerary(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<BookingRequest>()!;

        // Step 1: search — checkpoint saved after completion
        var flights = await context.CallActivityAsync<FlightSearchResult>(
            nameof(SearchFlightsActivity), request.FlightCriteria);

        // Step 2: search hotels in parallel with no dependency on flight result
        var hotels = await context.CallActivityAsync<HotelSearchResult>(
            nameof(SearchHotelsActivity), request.HotelCriteria);

        // Step 3: process payment only after both searches complete
        var payment = await context.CallActivityAsync<PaymentResult>(
            nameof(ProcessPaymentActivity),
            new PaymentRequest(flights.SelectedFlight, hotels.SelectedHotel));

        return new BookingResult(flights, hotels, payment);
    }

    [Function(nameof(SearchFlightsActivity))]
    public async Task<FlightSearchResult> SearchFlightsActivity(
        [ActivityTrigger] FlightCriteria criteria,
        FunctionContext executionContext)
    {
        // In production, resolve the MCP client from DI via executionContext.InstanceServices.
        await Task.Delay(10); // placeholder for actual MCP tool invocation
        return new FlightSearchResult(new FlightOption(criteria.Origin, criteria.Destination, criteria.Date));
    }

    [Function(nameof(SearchHotelsActivity))]
    public async Task<HotelSearchResult> SearchHotelsActivity(
        [ActivityTrigger] HotelCriteria criteria,
        FunctionContext executionContext)
    {
        await Task.Delay(10);
        return new HotelSearchResult(new HotelOption(criteria.City, criteria.CheckIn));
    }

    [Function(nameof(ProcessPaymentActivity))]
    public async Task<PaymentResult> ProcessPaymentActivity(
        [ActivityTrigger] PaymentRequest request,
        FunctionContext executionContext)
    {
        await Task.Delay(10);
        return new PaymentResult(Guid.NewGuid().ToString("N"), "CONFIRMED");
    }
}

// Input and output types for the orchestrator and activity functions.
public record BookingRequest(FlightCriteria FlightCriteria, HotelCriteria HotelCriteria);
public record FlightCriteria(string Origin, string Destination, DateOnly Date);
public record HotelCriteria(string City, DateOnly CheckIn, DateOnly CheckOut);
public record PaymentRequest(FlightOption SelectedFlight, HotelOption SelectedHotel);
public record BookingResult(FlightSearchResult Flights, HotelSearchResult Hotels, PaymentResult Payment);
public record FlightSearchResult(FlightOption SelectedFlight);
public record HotelSearchResult(HotelOption SelectedHotel);
public record PaymentResult(string TransactionId, string Status);
public record FlightOption(string Origin, string Destination, DateOnly Date);
public record HotelOption(string City, DateOnly CheckIn);
