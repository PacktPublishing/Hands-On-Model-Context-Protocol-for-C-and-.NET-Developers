# Chapter 11 — Quiz answers

1. Eliminate the SDK vulnerabilities by using a multi-stage Dockerfile that builds in an SDK image and publishes into a smaller runtime-only image. The final image becomes smaller because the build tools and SDK layer are excluded from the shipped container.

2. The delay is a cold-start penalty caused by scaling to zero. Use a minimum replica count of 1 for interactive search traffic, or switch to a hosting option that keeps warm instances; the trade-off is higher idle cost in exchange for lower latency.

3. This is a major version increment because the new tool combines booking cancellation and refund processing into one irreversible capability, which changes the behavior and risk profile of consumers. Durable Functions orchestration is appropriate only if the operation truly spans multiple steps or needs checkpointed recovery; a single atomic capability usually does not need it.

4. Allowing any branch to deploy to production creates a broad supply-chain and privilege-escalation risk, because untrusted or experimental branches can publish live changes. Restrict the federated credential to the production branch or a tightly controlled release branch and environment.

5. The correct immediate action is to stop or roll back the canary and keep the healthy revision serving traffic. Diagnose the failure by checking logs, health probes, dependency errors, and configuration diffs; before retrying, the new revision must pass soak, health, and error-rate checks at the canary stage with no unresolved 503 cause.
