// Chapter 9 — Section 9.4.3
// Streaming component with distributed trace correlation and UX metric recording.
// ActivitySource propagates W3C trace context through HttpClient headers to the MCP server,
// linking the Blazor component's root span to the server's handler span in Application Insights.
// TimeToFirstToken is recorded when the first non-null update.Text arrives.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Diagnostics;
using System.Text;

namespace TravelBooking.Blazor.Components;

public sealed partial class TracedFlightSearch : ComponentBase, IDisposable
{
    [Inject] private IChatClient ChatClient { get; set; } = null!;
    [Inject] private McpClient McpClient { get; set; } = null!;
    [Inject] private McpErrorHandler ErrorHandler { get; set; } = null!;

    private static readonly ActivitySource ActivitySource =
        new("TravelBooking.Blazor.Components");

    private readonly StringBuilder _responseText = new();
    private readonly List<ChatMessage> _messages = [];
    private CancellationTokenSource? _cts;
    private bool _isSearching;
    private McpUiError? _error;
    private string? _sessionId;

    protected override void OnInitialized() =>
        _sessionId = Guid.NewGuid().ToString("N")[..8];

    private async Task SearchAsync(string userQuery)
    {
        _cts = new CancellationTokenSource();
        _responseText.Clear();
        _error = null;
        _isSearching = true;

        using var activity = ActivitySource.StartActivity("ux.flight_search");
        activity?.SetTag("user.session_id", _sessionId);
        var searchStart = Stopwatch.GetTimestamp();
        double? firstTokenMs = null;

        try
        {
            if (_messages.Count == 0)
                _messages.Add(new ChatMessage(ChatRole.System,
                    "You are a travel booking assistant."));
            _messages.Add(new ChatMessage(ChatRole.User, userQuery));

            var tools = await McpClient.ListToolsAsync(_cts.Token);
            var options = new ChatOptions { Tools = [.. tools] };
            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in ChatClient
                .GetStreamingResponseAsync(_messages, options, _cts.Token))
            {
                if (update.Text is { Length: > 0 })
                {
                    if (firstTokenMs is null)
                    {
                        firstTokenMs = Stopwatch
                            .GetElapsedTime(searchStart).TotalMilliseconds;
                        UxMetrics.TimeToFirstToken.Record(firstTokenMs.Value,
                            new KeyValuePair<string, object?>("session", _sessionId));
                        activity?.SetTag("ux.first_token_ms", firstTokenMs.Value);
                    }
                    _responseText.Append(update.Text);
                }
                updates.Add(update);
                StateHasChanged();
            }

            _messages.AddMessages(updates);

            var totalMs = Stopwatch.GetElapsedTime(searchStart).TotalMilliseconds;
            UxMetrics.TimeToComplete.Record(totalMs,
                new KeyValuePair<string, object?>("session", _sessionId));
            activity?.SetTag("ux.total_ms", totalMs);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _error = ErrorHandler.Classify(ex);
            UxMetrics.ErrorCount.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        finally
        {
            _isSearching = false;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
