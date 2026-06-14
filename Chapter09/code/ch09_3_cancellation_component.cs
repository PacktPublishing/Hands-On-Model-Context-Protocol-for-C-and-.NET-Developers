// Chapter 9 — Section 9.1.3
// Cancellation-safe Blazor component pattern.
// CancellationTokenSource is cancelled both from the Cancel button and from Dispose()
// so that navigating away while a stream is active stops the LLM call immediately.
// The disabled binding prevents ObjectDisposedException on rapid click sequences.

// This file shows the @code section and Razor markup fragment.
// Wire into a .razor file with @inherits ComponentBase and @implements IDisposable.

using Microsoft.AspNetCore.Components;

namespace TravelBooking.Blazor.Components;

// Razor markup excerpt — include in the .razor template:
//
//   <button @onclick="CancelSearch" disabled="@(!_isSearching)">Cancel</button>
//   <button @onclick="() => SearchAsync(_query)" disabled="@_isSearching">Search</button>

public abstract class CancellationAwareComponent : ComponentBase, IDisposable
{
    protected CancellationTokenSource? Cts;
    protected bool IsWorking;

    protected void BeginOperation()
    {
        Cts?.Dispose();
        Cts = new CancellationTokenSource();
        IsWorking = true;
    }

    protected void Cancel() => Cts?.Cancel();

    protected void EndOperation()
    {
        IsWorking = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        // Cancel any active operation when the user navigates away.
        Cts?.Cancel();
        Cts?.Dispose();
    }
}

// Concrete component that inherits the cancellation pattern.
public sealed partial class BookingWorkflow : CancellationAwareComponent
{
    [Inject] private Microsoft.Extensions.AI.IChatClient ChatClient { get; set; } = null!;
    [Inject] private ModelContextProtocol.Client.McpClient McpClient { get; set; } = null!;

    private string _query = string.Empty;

    private async Task SearchAsync(string userQuery)
    {
        BeginOperation();
        try
        {
            var tools = await McpClient.ListToolsAsync(Cts!.Token);
            var options = new Microsoft.Extensions.AI.ChatOptions { Tools = [.. tools] };
            var messages = new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.User, userQuery)
            };

            var updates = new List<Microsoft.Extensions.AI.ChatResponseUpdate>();
            await foreach (var update in ChatClient
                .GetStreamingResponseAsync(messages, options, Cts.Token))
            {
                updates.Add(update);
                StateHasChanged();
            }
            messages.AddMessages(updates);
        }
        catch (OperationCanceledException) { }
        finally { EndOperation(); }
    }
}
