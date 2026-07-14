# Chapter 5 — Quiz answers

1. `[EnumeratorCancellation]` tells the async iterator infrastructure to flow the caller's cancellation token into the generated enumerator so enumeration can stop promptly. If you omit it, cancellation passed by the caller may not reach the iterator body, and the stream can keep producing values longer than intended.

2. Reflection works because the validation service only needs to inspect the server's public tool metadata and attributes; it does not need the server runtime itself. `IMcpServer` is not an option because the tests are validating contract shape without depending on a live server instance or transport.

3. `McpException` represents an MCP-level tool or protocol failure that should be surfaced as a normal tool error, while `McpProtocolException` indicates a protocol violation such as malformed messages or handshake issues. Use `McpException` for business or validation failures, and `McpProtocolException` when the client/server exchange itself is invalid.

4. Booking is often more failure-sensitive and irreversible than search, so it should retry less aggressively. Search can tolerate more retries because it is read-only, while booking should avoid duplicate side effects and repeated payment attempts.

5. No, the circuit breaker does not open because the minimum throughput threshold is not met. With only three requests, the breaker does not have enough volume to evaluate the failure ratio, so the failures are recorded but the breaker remains closed.
