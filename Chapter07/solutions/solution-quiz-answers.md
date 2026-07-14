# Chapter 7 — Quiz answers

1. Catch the MCP handshake and transport-related exceptions, including `McpException`, because initialization occurs over the same protocol surface as normal tool calls and can fail for protocol reasons. `McpException` belongs there because the server may reject the handshake or return an MCP-level error even before the client is fully initialized.

2. If you do not `await using` the disposable, the subscription remains active and the resource continues to hold server-side notification state. That causes a leak at the subscription layer and keeps notifications flowing to a handler that should have been detached, potentially duplicating callbacks or preventing cleanup.

3. `DropOldest` discards the oldest queued progress item when the channel is full, keeping the newest updates moving. `Wait` would block the callback, but `Progress<T>` is synchronous and `TryWrite` cannot await; that would deadlock or stall the callback path instead of applying backpressure correctly.

4. The distributed cache and persisted workflow context let the coordinator reconstruct the last completed step and resume from the correct point after restart. The confirmed booking reference survives in the cache/state store, so the workflow can continue with the itinerary read rather than starting over.

5. It should not be retried, because the message indicates a validation error rather than a transient transport failure. Exclude non-transient validation failures by checking the exception payload or message and only retrying when the failure is transport-related, timeout-related, or otherwise explicitly transient.
