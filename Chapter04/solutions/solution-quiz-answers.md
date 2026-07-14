# Chapter 4 — Quiz answers

1. The most relevant heuristic is the release cadence heuristic: the boundary should favor the aggregate with the higher change rate and isolate it from the slower-moving aggregate. Similar SLAs alone are not enough to justify a shared boundary when release cadences diverge significantly.

2. The synchronous cross-server call creates a temporal coupling and distributed transaction boundary where FlightsServer depends on ItineraryServer's runtime availability. The correct resolution is to publish the payment result asynchronously, for example through an event or message, so the payment path does not block on the downstream server.

3. The distribution pattern is fan-out followed by aggregation, and the combined latency budget is the maximum of the parallel branches rather than the sum, so it produces about 400 ms to 500 ms plus coordination overhead. In practice the FlightsServer branch at 400 ms dominates the overall path.

4. A synchronous call would force PaymentsServer's 99.99% availability to inherit ItineraryServer's lower 99.95% availability and tighter failure characteristics. The resolving pattern is asynchronous integration via messaging or an outbox so PaymentsServer can commit its own work without depending on the downstream server's live availability.

5. Two likely causes are an environment mismatch and test data or timing differences. For example, CI may use a different configuration, missing secret, or slower timing profile; another common cause is external dependency variability such as an unseeded mock, a shared test database, or a brittle timeout assumption.
