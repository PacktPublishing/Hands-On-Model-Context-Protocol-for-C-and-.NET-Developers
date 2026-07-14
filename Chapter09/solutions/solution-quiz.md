# Chapter 9 — Quiz questions

1. A Blazor component subscribes to `JobStatusStore.OnStatusChanged` and calls `StateHasChanged()` directly in the handler. The job status display does not update when background jobs complete. Explain why, and describe the correct fix.

2. The `CachingMcpClient` decorator caches `book_flight` responses alongside `search_flights` responses. Describe the user-visible bug this produces and the code change that prevents it.

3. Your background job processor creates `ReActOrchestrator` as a singleton and reuses the same instance across all jobs. Describe the failure mode this produces and explain why creating a new `IServiceScope` per job prevents it.

4. `GetStreamingResponseAsync` runs on the Blazor component's synchronization context. Explain why `StateHasChanged()` is safe without `InvokeAsync` inside the `await foreach` loop, but `InvokeAsync(StateHasChanged)` is required in a `Progress<T>` callback from a `BackgroundService`.

5. The offline retry queue replays `book_flight` before `search_flights` because both were submitted within the same second and the database returns them in primary key insertion order. Describe the failure and the query change that prevents it.

6. A user reports that the offline indicator appears in their browser but is not announced by their screen reader when connectivity drops. Identify the missing markup attribute and explain what value it should have for an urgent connectivity announcement.
