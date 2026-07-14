# Chapter 4 — Quiz questions

1. You have two aggregates owned by a single team, with similar SLA targets, but significantly different release cadences. Which granularity heuristic is most relevant, and what does it suggest about the boundary decision?

2. A FlightsServer handler calls ItineraryServer synchronously to verify booking status before confirming a seat. Identify the architectural problem this creates and describe the correct resolution.

3. A combined travel search requires results from FlightsServer at 400ms p95 and HotelsServer at 350ms p95, and the overall combined latency budget is 500ms. Which distribution pattern applies, and what latency does it produce?

4. After processing a payment, PaymentsServer must update ItineraryServer with the payment result. PaymentsServer has a 99.99% availability target and ItineraryServer runs at 99.95%. Explain why a synchronous cross-server call is incompatible with the PaymentsServer target, and name the pattern that resolves the incompatibility.

5. An SLA acceptance test for `SearchFlightsTool` passes consistently in a local development environment but fails in CI. Name two likely causes that relate to the test environment rather than to the server implementation.
