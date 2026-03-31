// Chapter 5 — Section 5.3.1
// SearchFlightsStreaming returns IAsyncEnumerable<FlightOption>.
// The SDK serialises each yielded item as a streamed tool response chunk.
// [EnumeratorCancellation] is required so that client disconnection
// propagates through the await foreach and stops iteration immediately.

using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

[McpServerToolType]
public sealed partial class FlightTools
{
    [McpServerTool, Description(
        "Stream available flight options as results arrive from each airline partner. " +
        "Results are emitted progressively — the client receives the first options " +
        "within seconds rather than waiting for all airlines to respond.")]
    public async IAsyncEnumerable<FlightOption> SearchFlightsStreaming(
        [Description("IATA origin airport code (e.g. LHR)")] string origin,
        [Description("IATA destination airport code (e.g. JFK)")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _searchService.OpenResultChannel(
            origin, destination, departureDate, cancellationToken);

        await foreach (var option in channel.ReadAllAsync(cancellationToken))
        {
            yield return option;
        }
    }
}
