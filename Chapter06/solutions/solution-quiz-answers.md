# Chapter 6 — Quiz answers

1. The assertion is checking the wrong condition: `IsError` can be false even when the tool returned a logical failure object, depending on how the response is wrapped. Fix the test by asserting the specific success or payload contract that distinguishes a real successful tool result from an error result.

2. The developer must update the contract tests and any generated schema or expected snapshots that reflect the parameter name change, then run the suite so the breaking change is caught before merge. If the change is intentional, the tests and schema expectations should be updated together so the new contract is the authoritative one.

3. It is an improvement, not a regression, because lower allocations are better. The `[Benchmark(Baseline = true)]` marker makes the baseline method the reference point, and BenchmarkDotNet reports other methods relative to it so you can compare allocation and throughput deltas directly.

4. Two likely root causes are that the timeout policy is not actually wrapping the slow operation, or the test is measuring the wrong duration segment. Diagnose by confirming policy registration order and by instrumenting the actual call path to see where the 12-second delay occurs relative to the 8-second timeout.

5. FluentValidation enforces imperative and cross-field rules that JSON Schema cannot express cleanly, such as business logic based on multiple inputs or asynchronous checks. Layer 2 validates structure; Layer 1 validates richer domain rules before the MCP tool executes.
