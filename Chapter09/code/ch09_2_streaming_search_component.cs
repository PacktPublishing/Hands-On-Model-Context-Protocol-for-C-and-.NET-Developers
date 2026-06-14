// Chapter 9 — Section 9.1.2
// FlightSearch Blazor Server component that streams LLM responses progressively.
// GetStreamingResponseAsync returns IAsyncEnumerable<ChatResponseUpdate>.
// StateHasChanged() is safe inside await foreach because the loop runs on the component's
// synchronization context — no InvokeAsync needed here.

// This file shows the @code section logic; the full .razor file also contains
// the HTML template with the result display and Cancel button bindings.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text;

namespace TravelBooking.Blazor.Components;

public sealed partial class FlightSearch : ComponentBase, IDisposable
{
    [Inject] private IChatClient ChatClient { get; set; } = null!;
    [Inject] private McpClient McpClient { get; set; } = null!;

    private readonly List<ChatMessage> _messages = [];
    private readonly StringBuilder _responseText = new();
    private CancellationTokenSource? _cts;
    private bool _isSearching;

    private async Task SearchAsync(string userQuery)
    {
        _cts = new CancellationTokenSource();
        _responseText.Clear();
        _isSearching = true;
        var updates = new List<ChatResponseUpdate>();

        try
        {
            if (_messages.Count == 0)
                _messages.Add(new ChatMessage(ChatRole.System, BuildSystemPrompt()));

            _messages.Add(new ChatMessage(ChatRole.User, userQuery));

            var tools = await McpClient.ListToolsAsync(_cts.Token);
            var options = new ChatOptions { Tools = [.. tools] };

            await foreach (var update in ChatClient
                .GetStreamingResponseAsync(_messages, options, _cts.Token))
            {
                _responseText.Append(update.Text);
                updates.Add(update);
                StateHasChanged();
            }

            _messages.AddMessages(updates);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isSearching = false;
            StateHasChanged();
        }
    }

    private void CancelSearch() => _cts?.Cancel();

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private static string BuildSystemPrompt() => """
        You are a travel booking assistant. Use the available tools
        to fulfill flight search and booking requests.
        Always call search_flights before book_flight.
        Never invent argument values not present in the conversation.
        """;
}
