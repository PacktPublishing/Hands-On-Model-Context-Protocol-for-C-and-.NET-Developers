// Chapter 8 — Section 8.3.5
// Structured audit logger for the planning loop lifecycle.
// Records user input, tool calls with arguments, tool results, and the final response.
// All log entries call PiiRedactor.Redact before writing to prevent PII reaching log sinks.

namespace TravelBooking.Orchestration;

public sealed class AuditLogger(ILogger<AuditLogger> logger)
{
    public void LogUserInput(string sessionId, string userInput)
    {
        logger.LogInformation(
            "AUDIT [{Session}] UserInput input={Input}",
            sessionId,
            PiiRedactor.Redact(userInput));
    }

    public void LogToolCall(
        string sessionId,
        string toolName,
        IReadOnlyDictionary<string, object?> args)
    {
        logger.LogInformation(
            "AUDIT [{Session}] ToolCall {Tool} args={Args}",
            sessionId,
            toolName,
            PiiRedactor.Redact(JsonSerializer.Serialize(args)));
    }

    public void LogToolResult(
        string sessionId,
        string toolName,
        string result)
    {
        logger.LogInformation(
            "AUDIT [{Session}] ToolResult {Tool} result={Result}",
            sessionId,
            toolName,
            PiiRedactor.Redact(result));
    }

    public void LogFinalResponse(string sessionId, string response)
    {
        logger.LogInformation(
            "AUDIT [{Session}] FinalResponse response={Response}",
            sessionId,
            PiiRedactor.Redact(response));
    }
}
