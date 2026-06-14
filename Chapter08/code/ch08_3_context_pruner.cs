// Chapter 8 — Section 8.2.2
// Context pruner that trims the message history before each LLM call.
// Retains all system messages, the last user message, and the N most recent
// tool interaction pairs (assistant call + tool result).
// PinSessionFact preserves extracted values (flightId, booking references)
// in the system layer so they survive pruning of verbose tool results.

using Microsoft.Extensions.AI;

namespace TravelBooking.Orchestration;

public static class ContextPruner
{
    public static List<ChatMessage> Prune(
        List<ChatMessage> messages,
        int keepToolInteractions = 5)
    {
        var system = messages.Where(m => m.Role == ChatRole.System);
        var lastUser = messages.Last(m => m.Role == ChatRole.User);
        var recent = messages
            .Where(m => m.Role == ChatRole.Assistant || m.Role == ChatRole.Tool)
            .TakeLast(keepToolInteractions * 2);

        return [.. system, .. recent, lastUser];
    }

    // Pin a key/value session fact to a persistent system message so that
    // flightId, booking references, and similar intermediate values remain
    // available after verbose tool results are pruned from history.
    public static void PinSessionFact(
        List<ChatMessage> messages,
        string key,
        string value)
    {
        const string FactsHeader = "[Session facts]";
        var factsMsg = messages
            .FirstOrDefault(m =>
                m.Role == ChatRole.System &&
                m.Text?.StartsWith(FactsHeader) == true);

        if (factsMsg is null)
        {
            // Insert after the first system message (the role definition).
            var insertAt = Math.Min(1, messages.Count);
            messages.Insert(insertAt,
                new ChatMessage(ChatRole.System, $"{FactsHeader}\n{key}: {value}"));
            return;
        }

        var updated = factsMsg.Text + $"\n{key}: {value}";
        var idx = messages.IndexOf(factsMsg);
        messages[idx] = new ChatMessage(ChatRole.System, updated);
    }
}
