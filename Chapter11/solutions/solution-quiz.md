# Chapter 11 — Quiz questions

1. Your team deploys the Payments MCP server using a single-stage Dockerfile and a security scanner flags 47 vulnerabilities in the SDK tools included in the image. Describe the changes required to eliminate these vulnerabilities and explain why the final image size changes.

2. The Hotels MCP server is deployed to a Container App with a minimum replica count of 0. Users report intermittent 10-second delays on hotel search during off-peak hours. Identify the cause, propose a solution, and explain the cost trade-off.

3. You add a new `cancel_flight` tool to the Flights capability library. The tool cancels a booking and processes a refund in a single operation. Explain whether this change requires a major, minor, or patch version increment, and whether Durable Functions orchestration is appropriate for this operation.

4. A GitHub Actions workflow is configured with an Entra ID federated credential that allows any branch in the repository to deploy to production. Describe the security risk and the specific change needed to address it.

5. Your team uses blue/green deployment for the Payments MCP server. During the Phase 2 canary, 8% of payment requests fail with a 503 error. The remaining 92% of traffic is still on the previous revision and healthy. Describe the correct immediate action, the diagnostic steps, and the criteria that must be met before attempting a second deployment.
