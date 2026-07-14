# Chapter 8 — Quiz questions

1. `McpClientTool` implements `AIFunction`. Explain what this inheritance gives you when connecting an MCP server to an `IChatClient`, and why no adapter code is required.

2. `UseFunctionInvocation()` handles the planning loop automatically. Describe two scenarios where you should replace it with a manual loop and explain what the manual loop enables that the middleware cannot.

3. The system prompt does not embed tool names or schemas as free text, even though the LLM needs to know what tools are available. Explain how the LLM receives that information and why the native function-calling format is preferable to text injection.

4. `GuardrailRejectedException` is thrown inside `InvokeWithGuardrailsAsync`. Describe what happens if this exception is not caught and how you would handle it to give the user a meaningful response.

5. `UsageDetails.InputTokenCount` is `long?`. The `TokenTracker.Add` method uses `?? 0` rather than accessing `.Value` directly. Explain why `.Value` would be dangerous here and in which realistic scenario `InputTokenCount` might be null.

6. Your orchestrator's average iteration count is 7 against a target of fewer than 5. Propose two changes to the system prompt and one change to the guardrail configuration that could reduce the iteration count without lowering the task completion rate.
