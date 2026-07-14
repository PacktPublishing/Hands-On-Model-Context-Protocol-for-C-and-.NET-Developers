# Chapter 12 — Quiz questions

1. The MCP C# SDK registers four built-in OTel histograms. Name all four, state the unit they use, and explain which two you would examine first when investigating a latency SLO breach.

2. Explain the behavioral difference between `UseOtlpExporter()` and calling `AddOtlpExporter()` three times. Describe a deployment scenario where calling `AddOtlpExporter` separately per signal would be the correct choice.

3. `Diagnostics.TryGetOuterToolExecutionActivity(out Activity? activity)` returns `true` inside your instrumentation middleware. Describe precisely what your code should do next and why calling `s_source.StartActivity(...)` would produce incorrect traces in this case.

4. Your `ConsistentHashRouter` has five server instances, each registered with 150 virtual nodes. One instance is removed during a scale-in event. Approximately what fraction of tenant routing decisions change, and how does the virtual node count per server affect that fraction as the fleet grows?

5. A `BookFlightTool` call completes successfully and transitions the workflow to `AwaitingApprovalState`. The subsequent `PaymentChargeTool` call triggers `BudgetExceededException`. Describe the inconsistent state this creates and propose two design changes, one in the budget enforcer and one in the workflow orchestrator, that together prevent this class of failure.
