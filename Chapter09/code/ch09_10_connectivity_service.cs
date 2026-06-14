// Chapter 9 — Section 9.3.2
// PollingConnectivityService detects MCP server reachability via periodic health endpoint polling.
// Used in Blazor Server; the MAUI implementation subscribes to the platform's native event instead.
// The offline indicator component subscribes to OnConnectivityChanged via InvokeAsync(StateHasChanged).

using Microsoft.Extensions.Options;

namespace TravelBooking.Blazor.Services;

public sealed class ConnectivityOptions
{
    public string HealthPath { get; set; } = "/health";
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(15);
}

public sealed class PollingConnectivityService(
    HttpClient http,
    IOptions<ConnectivityOptions> opts,
    ILogger<PollingConnectivityService> logger)
    : IConnectivityService, IDisposable
{
    private bool _isOnline = true;
    private Timer? _timer;

    public bool IsOnline => _isOnline;
    public event Action<bool>? OnConnectivityChanged;

    public void Start() =>
        _timer = new Timer(
            PollAsync, null, TimeSpan.Zero, opts.Value.Interval);

    private async void PollAsync(object? _)
    {
        var prev = _isOnline;
        try
        {
            await http.GetAsync(opts.Value.HealthPath);
            _isOnline = true;
        }
        catch
        {
            _isOnline = false;
        }

        if (_isOnline != prev)
        {
            logger.LogInformation(
                "Connectivity changed: {State}", _isOnline ? "online" : "offline");
            OnConnectivityChanged?.Invoke(_isOnline);
        }
    }

    public void Dispose() => _timer?.Dispose();
}
