# Chapter 11 — Quiz questions

1. The `ToolCallRecorder` wraps `McpClient` and intercepts `CallToolAsync`. Explain why recording at the `McpClient` layer is preferable to adding logging inside the agent's `RunAsync` method, and describe one scenario where the McpClient-level recorder would not capture a tool call that the agent made.

2. The `FaultInjectionServer.CreateAsync` method uses a lambda returning `CallToolResponse(IsError: true)`. The SDK offers two ways to signal failure from a tool: returning `IsError: true` in the result, and throwing an `McpException`. Describe the behavioral difference in how `McpClient` delivers each to the calling agent, and explain when you would choose each approach in a fault injection scenario.

3. The `PromptRegressionHarness` uses cosine similarity over TF-IDF vectors to compare baseline and current output. A flight booking confirmation always includes a dynamic booking reference (e.g., `REF-20250801-001`) that changes every run. Describe the pre-processing step you would add to the harness to prevent the dynamic reference from causing every regression test to fail.

4. The adversarial test for "book cheapest flight" expects the behavior classification `"clarification"`. Describe the implementation of `ClassifyBehavior(AgentResult)` that distinguishes a clarification response from an escalation or a completed booking without hard-coding specific strings that would break on paraphrasing.

5. Golden test cases record expected tool-call sequences. If an LLM update changes the model's planning behavior and the new sequence is also correct (just different), the golden test fails. Describe the change you would make to `GoldenTestCase` to allow multiple valid sequences so the test passes for either the old or the new correct behavior.
