// Chapter 10 — Section 10.3.4
// Zero-downtime secret rotation using IOptionsMonitor<T>.OnChange.
// When the Key Vault configuration provider refreshes and detects a new
// secret version, OnChange fires and the HttpClient headers are updated
// without restarting the server or closing active SSE connections.
// Remove + Add avoids duplicate headers on rapid successive refreshes.

using Microsoft.Extensions.Options;

namespace TravelBooking.Integrations;

public sealed class AirlineApiOptions
{
    public string ApiKey  { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class AirlineApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly IDisposable? _changeToken;

    public AirlineApiClient(
        HttpClient http,
        IOptionsMonitor<AirlineApiOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri(options.CurrentValue.BaseUrl);
        SetApiKey(options.CurrentValue.ApiKey);

        // OnChange fires each time the Key Vault provider reloads
        // and the AirlineApi:Key secret value has changed.
        _changeToken = options.OnChange(updated => SetApiKey(updated.ApiKey));
    }

    private void SetApiKey(string key)
    {
        // Remove before Add to prevent duplicate X-Api-Key headers
        // when OnChange fires twice during a Key Vault refresh cycle.
        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        _http.DefaultRequestHeaders.Add("X-Api-Key", key);
    }

    public async Task<string> SearchFlightsAsync(
        string origin, string destination, DateOnly date,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"/flights?from={origin}&to={destination}&date={date:yyyy-MM-dd}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public void Dispose() => _changeToken?.Dispose();
}

// DI registration: AddHttpClient wires the typed client with IHttpClientFactory.
// AddOptions + Configure binds AirlineApiOptions from IConfiguration,
// which in turn reads from the Key Vault configuration provider.
//
// builder.Services
//     .AddHttpClient<AirlineApiClient>()
//     .ConfigureHttpClient(client => { /* base address set in constructor */ });
//
// builder.Services
//     .AddOptions<AirlineApiOptions>()
//     .BindConfiguration("AirlineApi")
//     .ValidateDataAnnotations();
