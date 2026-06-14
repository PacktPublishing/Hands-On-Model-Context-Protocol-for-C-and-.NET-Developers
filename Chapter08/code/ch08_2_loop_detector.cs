// Chapter 8 — Section 8.1.4
// Loop detector that tracks recent tool invocations and fires when repetition is detected.
// Hashes each call by tool name and serialized arguments; fires before the MaxIterations cap.
// Inject a course-correction system message when IsLooping returns true.

namespace TravelBooking.Orchestration;

public sealed class LoopDetector
{
    private readonly Queue<string> _recent = new();
    private readonly int _window;
    private readonly int _threshold;

    // window: number of recent calls to retain for comparison
    // threshold: how many identical calls within the window trigger detection
    public LoopDetector(int window = 6, int threshold = 2)
    {
        _window = window;
        _threshold = threshold;
    }

    public bool IsLooping(string toolName, string argsJson)
    {
        var key = $"{toolName}|{argsJson}";
        _recent.Enqueue(key);
        if (_recent.Count > _window) _recent.Dequeue();
        return _recent.Count(k => k == key) >= _threshold;
    }

    public void Reset() => _recent.Clear();
}
