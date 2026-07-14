# Chapter 6 — Quiz questions

1. You write an integration test that calls `search_flights` and asserts `Assert.False(result.IsError)`. The test passes even when the handler returns a tool error. What is the defect in the assertion and how do you fix it?

2. A pull request that renames the `departureDate` parameter to `travelDate` must pass the contract test suite. Describe the steps a developer must take to accomplish this correctly.

3. The BenchmarkDotNet output shows that `SearchFlights_WithResultCache` allocates 400 bytes per invocation compared to 620 bytes for the baseline. Is this a regression or an improvement? Explain the `[Benchmark(Baseline = true)]` mechanics that produce this comparison.

4. A chaos test configures `FaultOptions.DelayMs = 12000` but the test assertion `elapsed < TimeSpan.FromSeconds(10)` fails. The timeout policy is configured at 8 seconds. What are two possible root causes, and how would you diagnose each one?

5. An engineer argues that Layer 2 JSON Schema enforcement makes Layer 1 FluentValidation redundant. Explain why both layers are necessary and give one example of a constraint that only FluentValidation can enforce.
