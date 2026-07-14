# Chapter 8 — Quiz answers

1. `McpClientTool` implements `AIFunction`, so the LLM client can treat MCP tools as native callable functions without any adapter layer. The tool metadata, schema, and invocation path are exposed through the AI function abstraction automatically.

2. Replace `UseFunctionInvocation()` with a manual loop when you need custom planning, custom stop conditions, or intervention between iterations. A manual loop lets you inspect each model turn, inject guardrails, prune context, or choose tools dynamically in ways the built-in middleware does not expose.

3. The LLM receives tool availability through the native function-calling protocol and schema registration, not through prompt text. That is preferable because the model gets structured tool metadata that the runtime can validate, rather than brittle free-text instructions that can drift or be ignored.

4. If `GuardrailRejectedException` is not caught, it will bubble out as an unhandled failure and the user will see a generic error. Catch it at the orchestrator boundary, then map it to a user-facing message that explains the action was blocked by policy and, where appropriate, suggests a safer alternative.

5. `.Value` would throw if `InputTokenCount` is null, which can happen when token usage is unavailable for a provider response or when the API omits usage details for a particular call. Using `?? 0` preserves the accounting logic without failing on missing telemetry.

6. Two prompt changes are to narrow the task instructions and reduce ambiguous objectives, and one guardrail change is to add an earlier or stricter rejection threshold for low-confidence plans. Together these reduce unnecessary planning loops while keeping the model focused on the target outcome.
