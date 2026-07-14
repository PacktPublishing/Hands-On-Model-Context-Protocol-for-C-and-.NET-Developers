# Chapter 9 — Quiz answers

1. The Blazor event handler is not running on the component's synchronization context, so `StateHasChanged()` is being called from the wrong thread and the render update is discarded. The correct fix is to marshal the callback through `InvokeAsync(StateHasChanged)`.

2. The bug is that write operations such as `book_flight` are treated like cacheable reads, so the user can see a stale booking confirmation or duplicate a side effect from cached data. Prevent it by allowlisting only read-only tools in the cache and never caching write operations.

3. Reusing a singleton `ReActOrchestrator` allows workflow state, scoped services, or transport/session state from one job to leak into the next, causing protocol mismatches or incorrect behavior. Creating a new `IServiceScope` per job gives each job its own fresh scoped dependencies and isolates failures.

4. Inside the `await foreach` loop, the code is already executing on the Blazor component's synchronization context, so `StateHasChanged()` is safe. In a `BackgroundService` callback, the code runs on a thread pool thread, so `InvokeAsync(StateHasChanged)` is required to marshal back to the renderer.

5. The failure is that the booking request is replayed before the flight search that produced its `flightId`, so the server rejects the booking as missing or invalid. The fix is to order the replay query by submission time or another explicit causal order column instead of relying on insertion order.

6. The missing markup is `aria-live`, and the appropriate value for an urgent connectivity change is `assertive`. That tells assistive technologies to announce the offline state immediately rather than waiting for a calmer update cycle.
