// Chapter 5 — Section 5.2.3
// Startup validation using reflection to confirm every registered tool has both
// [McpServerToolAttribute] and a non-empty [Description] on the method.
// Throws InvalidOperationException at startup so misconfiguration is caught
// before any client connects. Does NOT use IMcpServer (that interface does not exist).

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

public sealed class CapabilityValidationService : IHostedService
{
    private readonly ILogger<CapabilityValidationService> _logger;

    public CapabilityValidationService(ILogger<CapabilityValidationService> logger)
        => _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var toolTypes = Assembly.GetEntryAssembly()!
            .GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);

        foreach (var type in toolTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

            foreach (var method in methods)
            {
                var desc = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrWhiteSpace(desc))
                    throw new InvalidOperationException(
                        $"Tool method '{type.Name}.{method.Name}' is missing a [Description]. " +
                        "All tools must describe their purpose for LLM discoverability.");

                _logger.LogInformation(
                    "Validated tool: {Type}.{Method}", type.Name, method.Name);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
