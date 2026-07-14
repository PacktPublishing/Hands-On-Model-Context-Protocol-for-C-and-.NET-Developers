# Chapter 5 — Quiz questions

1. What is the purpose of `[EnumeratorCancellation]` on a `CancellationToken` parameter in an `IAsyncEnumerable<T>` tool method, and what happens if you omit it?

2. The `CapabilityValidationService` uses reflection rather than `IMcpServer`. Why does this approach work, and why is `IMcpServer` not an option?

3. Explain the difference between `McpException` and `McpProtocolException`. Give one example scenario where each is the correct choice.

4. A booking pipeline is configured with `MaxRetryAttempts = 1` while a search pipeline uses `MaxRetryAttempts = 2`. What is the engineering reason for the asymmetry?

5. You configure a circuit breaker with `FailureRatio = 0.5` and `MinimumThroughput = 5`. The server receives three requests; all three fail. Does the circuit breaker open? Explain your reasoning.
