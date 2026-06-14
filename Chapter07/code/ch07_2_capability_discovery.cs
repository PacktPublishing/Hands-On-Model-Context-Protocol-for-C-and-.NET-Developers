// Chapter 7 — Section 7.1.2
// Runtime capability discovery: ListToolsAsync, ListResourcesAsync, and dynamic
// tool-list change handling via RegisterNotificationHandler.
// Check ServerCapabilities before calling list methods — a server without tool support
// returns a protocol error on ListToolsAsync.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TravelBooking.Client;

public sealed class CapabilityDiscoveryService : IAsyncDisposable
{
    private readonly McpClient _client;
    private IAsyncDisposable? _toolChangeHandler;
    private IList<McpClientTool> _tools = [];
    private IList<McpClientResource> _resources = [];

    public CapabilityDiscoveryService(McpClient client) => _client = client;

    public IReadOnlyList<McpClientTool> Tools => (IReadOnlyList<McpClientTool>)_tools;
    public IReadOnlyList<McpClientResource> Resources => (IReadOnlyList<McpClientResource>)_resources;

    // Register tool-change handler before the first list call to avoid a race condition.
    // The server can send notifications/tools/list_changed at any time after initialize.
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _toolChangeHandler = _client.RegisterNotificationHandler(
            NotificationMethods.ToolsListChangedNotification,
            async (_, ct) =>
            {
                _tools = await _client.ListToolsAsync(cancellationToken: ct);
            });

        if (_client.ServerCapabilities?.Tools is not null)
            _tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);

        if (_client.ServerCapabilities?.Resources is not null)
        {
            _resources = await _client.ListResourcesAsync(cancellationToken: cancellationToken);

            var templates = await _client.ListResourceTemplatesAsync(
                cancellationToken: cancellationToken);
        }
    }

    public McpClientTool? FindTool(string name)
        => _tools.FirstOrDefault(t => t.Name == name);

    public bool HasTool(string name)
        => _tools.Any(t => t.Name == name);

    public async ValueTask DisposeAsync()
    {
        if (_toolChangeHandler is not null)
            await _toolChangeHandler.DisposeAsync();
    }
}
