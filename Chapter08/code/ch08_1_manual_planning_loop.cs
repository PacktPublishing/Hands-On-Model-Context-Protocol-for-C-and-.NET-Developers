// Chapter 8 — Section 8.1.3
// Manual ReAct planning loop with guardrail interception and audit logging.
// Replaces UseFunctionInvocation() when tool calls must be inspected before they execute.
// InvokeWithGuardrailsAsync runs the guardrail pipeline, dispatches the matched McpClientTool,
// and appends FunctionResultContent so the LLM sees the result on the next iteration.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;
using TravelBooking.CodeSamples.Shared;
using TravelBooking.Orchestration.Guardrails;

namespace TravelBooking.Orchestration;

public sealed class ReActOrchestrator(
    IChatClient chatClient,
    McpClient mcpClient,
    GuardrailPipeline guardrails,
    AuditLogger audit,
    ILogger<ReActOrchestrator> logger)
{
    private const int MaxIterations = 10;

    public async Task<string> RunAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        audit.LogUserInput(sessionId, userMessage);

        var tools = await mcpClient.ListToolsAsync(cancellationToken);
        var toolMap = tools.ToDictionary(t => t.Name);
        var loopDetector = new LoopDetector();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt()),
            new(ChatRole.User, userMessage)
        };
        var options = new ChatOptions { Tools = [.. tools] };

        var iteration = 0;
        while (iteration++ < MaxIterations)
        {
            var pruned = ContextPruner.Prune(messages);
            var response = await chatClient.GetResponseAsync(
                pruned, options, cancellationToken);
            messages.AddRange(response.Messages);

            if (response.FinishReason == ChatFinishReason.Length)
            {
                logger.LogWarning("Context window exhausted at iteration {N}", iteration);
                break;
            }

            if (response.FinishReason != ChatFinishReason.ToolCalls)
                break;

            foreach (var msg in response.Messages)
            foreach (var call in msg.Contents.OfType<FunctionCallContent>())
            {
                var argsJson = JsonSerializer.Serialize(call.Arguments);
                if (loopDetector.IsLooping(call.Name, argsJson))
                {
                    messages.Add(new ChatMessage(ChatRole.System,
                        "You have called the same tool with the same " +
                        "arguments more than once. Summarize what you " +
                        "know so far and give your best answer."));
                    goto Done;
                }

                await InvokeWithGuardrailsAsync(
                    call, toolMap, messages, sessionId, cancellationToken);
            }
        }

        Done:
        var finalText = messages
            .LastOrDefault(m => m.Role == ChatRole.Assistant)
            ?.Text ?? string.Empty;
        audit.LogFinalResponse(sessionId, finalText);
        return finalText;
    }

    private async Task InvokeWithGuardrailsAsync(
        FunctionCallContent call,
        Dictionary<string, McpClientTool> toolMap,
        List<ChatMessage> messages,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var args = call.Arguments is not null
            ? call.Arguments.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
            : new Dictionary<string, object?>();

        try
        {
            await guardrails.AssertAllowedAsync(call.Name, args, cancellationToken);
            audit.LogToolCall(sessionId, call.Name, args);

            if (!toolMap.TryGetValue(call.Name, out var tool))
                throw new InvalidOperationException(
                    $"Tool '{call.Name}' not found in server capability list.");

            var result = await tool.InvokeAsync(call.Arguments ?? [], cancellationToken);
            var resultText = result?.ToString() ?? string.Empty;
            audit.LogToolResult(sessionId, call.Name, resultText);

            messages.Add(new ChatMessage(ChatRole.Tool,
                [new FunctionResultContent(call.CallId, call.Name, resultText)]));
        }
        catch (GuardrailRejectedException ex)
        {
            logger.LogWarning("Guardrail blocked {Tool}: {Reason}",
                call.Name, ex.RejectionReason);
            messages.Add(new ChatMessage(ChatRole.Tool,
                [new FunctionResultContent(call.CallId, call.Name,
                    $"Action blocked: {ex.RejectionReason}")]));
        }
    }

    private static string BuildSystemPrompt() => """
        You are a travel booking assistant. Use the available tools
        to fulfill flight search and booking requests.

        Rules:
        1. Always call search_flights before book_flight.
        2. Use the flightId returned by search_flights when calling book_flight.
        3. Request user confirmation before booking.
        4. Never invent argument values not present in the conversation.
        5. If a tool returns an error, explain it to the user and ask for clarification.
        """;
}
