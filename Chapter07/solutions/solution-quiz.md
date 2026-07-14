# Chapter 7 — Quiz questions

1. `McpClient.CreateAsync` performs the MCP initialize handshake internally. What exception types should you catch when calling it against an HTTP server, and why is `McpException` among them?

2. You subscribe to `travel://itineraries/B001` using `SubscribeToResourceAsync` with a handler, but do not `await using` the returned disposable. Describe exactly what goes wrong, covering both the subscription layer and the notification layer.

3. A tool call emits progress notifications at 500 updates per second and the application processes them at 50 per second. Explain what `BoundedChannelFullMode.DropOldest` does when the channel is full, and explain why `BoundedChannelFullMode.Wait` would not work correctly with `TryWrite` in a synchronous `Progress<T>` callback.

4. The workflow coordinator crashes after `BookFlightStepAsync` produces a confirmed booking reference but before `ReadItineraryStepAsync` runs. Describe how `IDistributedCache` and the `BookingWorkflowContext` state allow the coordinator to resume correctly after restart.

5. The Polly retry `ShouldHandle` predicate includes `Handle<McpException>()`. A `search_flights` call returns `McpException` with a message "Validation failed: origin must be a 3-letter IATA code." Should this be retried? Modify the predicate to exclude non-transient validation errors.
