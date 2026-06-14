// Chapter 8 — Section 8.4.3
// Per-iteration latency profiler for the planning loop.
// Uses ActivitySource for distributed trace correlation and Stopwatch for segment timing.
// Segments: LLM inference, MCP tool execution, client-side overhead (guardrails + pruning).

using System.Diagnostics;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Orchestration;

public sealed class LatencyProfiler(
    IChatClient chatClient,
    McpClient mcpClient,
    GuardrailPipeline guardrails)
{
    private static readonly ActivitySource ActivitySource =
        new("TravelBooking.Orchestrator");

    public async Task<(string Response, IterationProfile[] Iterations)>
        RunWithProfilingAsync(
            List<ChatMessage> messages,
            ChatOptions options,
            CancellationToken cancellationToken = default)
    {
        using var rootActivity = ActivitySource.StartActivity("orchestrator.run");

        var profiles = new List<IterationProfile>();
        var iteration = 0;

        while (iteration++ < 10)
        {
            using var iterActivity = ActivitySource.StartActivity(
                "orchestrator.iteration");
            iterActivity?.SetTag("iteration", iteration);

            // ── LLM inference ────────────────────────────────────────────────
            var llmStart = Stopwatch.GetTimestamp();
            var response = await chatClient.GetResponseAsync(
                messages, options, cancellationToken);
            var llmMs = Stopwatch.GetElapsedTime(llmStart).TotalMilliseconds;

            iterActivity?.SetTag("llm.latency_ms", llmMs);
            iterActivity?.SetTag("llm.input_tokens", response.Usage?.InputTokenCount);
            iterActivity?.SetTag("llm.finish_reason", response.FinishReason?.ToString());
            messages.AddRange(response.Messages);

            if (response.FinishReason != ChatFinishReason.ToolCalls)
            {
                profiles.Add(new IterationProfile(iteration, llmMs, 0, 0));
                break;
            }

            // ── MCP tool execution ───────────────────────────────────────────
            var toolMs = 0.0;
            var guardMs = 0.0;
            foreach (var msg in response.Messages)
            foreach (var call in msg.Contents.OfType<FunctionCallContent>())
            {
                var args = call.Arguments?.ToDictionary(k => k.Key, v => (object?)v.Value)
                    ?? new Dictionary<string, object?>();

                var guardStart = Stopwatch.GetTimestamp();
                await guardrails.AssertAllowedAsync(call.Name, args, cancellationToken);
                guardMs += Stopwatch.GetElapsedTime(guardStart).TotalMilliseconds;

                var toolStart = Stopwatch.GetTimestamp();
                var tool = options.Tools?.OfType<McpClientTool>()
                    .FirstOrDefault(t => t.Name == call.Name);
                if (tool is not null)
                    await tool.InvokeAsync(call.Arguments ?? [], cancellationToken);
                toolMs += Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;
            }

            iterActivity?.SetTag("tool.latency_ms", toolMs);
            iterActivity?.SetTag("guard.latency_ms", guardMs);
            profiles.Add(new IterationProfile(iteration, llmMs, toolMs, guardMs));
        }

        var finalText = messages
            .LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text ?? string.Empty;
        return (finalText, profiles.ToArray());
    }
}

public sealed record IterationProfile(
    int Iteration,
    double LlmMs,
    double ToolMs,
    double GuardMs)
{
    public double TotalMs => LlmMs + ToolMs + GuardMs;
}
