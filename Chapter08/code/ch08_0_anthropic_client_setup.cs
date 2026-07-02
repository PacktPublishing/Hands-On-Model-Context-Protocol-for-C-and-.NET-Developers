// Chapter08/code/ch08_0_anthropic_client_setup.cs
//
// Complete startup registration for the Anthropic C# SDK: DI wiring,
// a Polly resilience pipeline scoped to the exception types the SDK
// throws, and OpenTelemetry tracing for every LLM call the orchestrator
// makes in the rest of this chapter.
//
// Prerequisites (see "Technical requirements" earlier in this chapter):
//   dotnet add package Anthropic --version 10.*
//   dotnet add package Microsoft.Extensions.AI --version 9.3.*
//   dotnet add package Polly.Extensions
//   dotnet add package OpenTelemetry.Extensions.Hosting
//   dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol

using Anthropic;
using Anthropic.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;

namespace TravelBooking.Client.Anthropic;

/// <summary>
/// Registers an <see cref="IChatClient"/> backed by the Anthropic C# SDK,
/// wrapped with a Polly resilience pipeline and OpenTelemetry
/// instrumentation. Call <see cref="AddAnthropicChatClient"/> once from
/// Program.cs; every orchestrator component in this chapter resolves
/// IChatClient from the container afterward.
/// </summary>
public static class AnthropicServiceCollectionExtensions
{
    /// <summary>
    /// The ActivitySource name used for orchestrator-level tracing.
    /// Register this name with UseOpenTelemetry / AddSource so spans for
    /// individual LLM calls show up in your exporter of choice.
    /// </summary>
    public const string ActivitySourceName = "TravelBooking.Orchestrator";

    public static IServiceCollection AddAnthropicChatClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- 1. Register the raw AnthropicClient -----------------------
        //
        // ANTHROPIC_API_KEY is read automatically by the parameterless
        // constructor. Config/Key Vault values take precedence when
        // present so the same code works locally and in production.
        services.AddSingleton(_ =>
        {
            var configuredKey = configuration["Anthropic:ApiKey"];

            return string.IsNullOrEmpty(configuredKey)
                ? new AnthropicClient()
                : new AnthropicClient { ApiKey = configuredKey };
        });

        // --- 2. Build the Polly resilience pipeline ---------------------
        //
        // Retries on rate limiting and transient 5xx upstream errors.
        // AnthropicUnauthorizedException is deliberately NOT retried -
        // a bad or revoked API key will not fix itself on the next
        // attempt, and retrying it just burns time before the same
        // failure surfaces to the caller.
        services.AddSingleton(BuildResiliencePipeline);

        // --- 3. Register the IChatClient pipeline -----------------------
        //
        // Order matters: function invocation must sit above the
        // resilience wrapper so a retried call re-enters the full
        // tool-calling loop, and OpenTelemetry sits outermost so it
        // captures retries as part of the same logical operation.
        services.AddSingleton<IChatClient>(sp =>
        {
            var anthropicClient = sp.GetRequiredService<AnthropicClient>();
            var pipeline = sp.GetRequiredService<ResiliencePipeline>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            IChatClient baseClient = anthropicClient
                .AsIChatClient(Model.ClaudeSonnet4_6);

            IChatClient resilientClient =
                new ResilientChatClient(baseClient, pipeline,
                    sp.GetRequiredService<ILogger<ResilientChatClient>>());

            return resilientClient
                .AsBuilder()
                .UseFunctionInvocation()
                .UseOpenTelemetry(
                    sourceName: ActivitySourceName,
                    loggerFactory: loggerFactory)
                .Build();
        });

        return services;
    }

    private static ResiliencePipeline BuildResiliencePipeline(
        IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<
            ILogger<ResiliencePipeline>>();

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<AnthropicRateLimitException>()
                    .Handle<Anthropic5xxException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
                DelayGenerator = args =>
                {
                    // Honor the server-supplied Retry-After header when
                    // the SDK surfaces one for rate-limit responses.
                    if (args.Outcome.Exception
                        is AnthropicRateLimitException { RetryAfter: { } retryAfter })
                    {
                        return ValueTask.FromResult<TimeSpan?>(retryAfter);
                    }

                    return ValueTask.FromResult<TimeSpan?>(null);
                },
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retrying Anthropic call (attempt {Attempt}) " +
                        "after {Delay} due to {Exception}.",
                        args.AttemptNumber + 1,
                        args.RetryDelay,
                        args.Outcome.Exception?.GetType().Name);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    /// <summary>
    /// Registers OpenTelemetry tracing for the orchestrator's
    /// ActivitySource plus the sources Microsoft.Extensions.AI and the
    /// Anthropic SDK publish to. Exports via OTLP; swap the exporter for
    /// Application Insights or Console as needed in your environment.
    /// </summary>
    public static IServiceCollection AddAnthropicTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "TravelBooking.Orchestrator"))
            .WithTracing(tracing => tracing
                .AddSource(ActivitySourceName)
                .AddSource("Experimental.Microsoft.Extensions.AI")
                .AddSource("Anthropic.*")
                .AddOtlpExporter(otlp =>
                {
                    var endpoint = configuration["OpenTelemetry:Endpoint"];
                    if (!string.IsNullOrEmpty(endpoint))
                    {
                        otlp.Endpoint = new Uri(endpoint);
                    }
                }));

        return services;
    }
}

/// <summary>
/// Wraps an inner <see cref="IChatClient"/> so every call executes
/// through a Polly <see cref="ResiliencePipeline"/>. Kept as a thin
/// DelegatingChatClient rather than baked into the Anthropic SDK
/// registration so the retry policy can be unit tested in isolation
/// from the HTTP layer.
/// </summary>
public sealed class ResilientChatClient(
    IChatClient innerClient,
    ResiliencePipeline pipeline,
    ILogger<ResilientChatClient> logger)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await pipeline.ExecuteAsync(
                async ct => await base.GetResponseAsync(
                    messages, options, ct),
                cancellationToken);
        }
        catch (AnthropicUnauthorizedException)
        {
            logger.LogError(
                "ANTHROPIC_API_KEY is missing or revoked. " +
                "Not retrying - check the key and redeploy.");
            throw;
        }
    }

    public override IAsyncEnumerable<ChatResponseUpdate>
        GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
    {
        // Streaming responses are intentionally NOT wrapped in the retry
        // pipeline: once the first token has reached the caller, replaying
        // the call from scratch would duplicate output the UI has already
        // rendered. If the initial connection fails before any chunk
        // arrives, the exception surfaces immediately and the caller (or
        // an outer non-streaming retry at the workflow level) decides
        // whether to restart the request.
        try
        {
            return base.GetStreamingResponseAsync(
                messages, options, cancellationToken);
        }
        catch (AnthropicUnauthorizedException)
        {
            logger.LogError(
                "ANTHROPIC_API_KEY is missing or revoked. " +
                "Not retrying - check the key and redeploy.");
            throw;
        }
    }
}

// ---------------------------------------------------------------------
// Program.cs usage
// ---------------------------------------------------------------------
//
// var builder = WebApplication.CreateBuilder(args);
//
// builder.Services.AddAnthropicChatClient(builder.Configuration);
// builder.Services.AddAnthropicTracing(builder.Configuration);
//
// var app = builder.Build();
//
// Every component from this point in the chapter - the manual planning
// loop, the guardrail pipeline, and OrchestratorMetrics - resolves
// IChatClient from the container and gets retry handling and tracing
// for free.
