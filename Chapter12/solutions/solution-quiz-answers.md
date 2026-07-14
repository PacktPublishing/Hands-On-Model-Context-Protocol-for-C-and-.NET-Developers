# Chapter 12 — Quiz answers

1. The four built-in OTel histograms are request duration, prompt tokens, completion tokens, and total tokens, all measured in the units defined by the SDK for latency or token counts. The first two to inspect for a latency SLO breach are request duration and any span or tool-execution latency histogram that isolates server-side execution time, because together they show whether the slowdown is in transport or in the MCP handler itself.

2. `UseOtlpExporter()` configures the OTLP exporter for the default signal set in one place, while calling `AddOtlpExporter()` separately lets you target different exporters or endpoints per signal. You would split them when, for example, traces go to one backend and metrics or logs go to another, or when each signal needs different auth or sampling settings.

3. If `TryGetOuterToolExecutionActivity` returns true, you should annotate or use that returned `Activity` rather than starting a new outer activity. Starting a new one would create a nested or duplicate trace root and break the existing causal chain, whereas reusing the outer activity preserves the correct parent-child structure.

4. Roughly one-fifth of the routing decisions change when a single instance is removed, assuming a uniform consistent-hashing ring with enough virtual nodes to smooth distribution. More virtual nodes improve balance and reduce variance across tenants, especially as the fleet grows.

5. The workflow is in an inconsistent state because the booking succeeded and the approval boundary was crossed, but payment failed after the approval transition. One fix is to make the budget enforcer reject earlier before state transition, and another is to have the orchestrator stage irreversible work so it does not transition into the post-booking state until payment and budget checks have both succeeded.
