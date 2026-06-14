// Chapter 9 — Section 9.2.4
// Job processor with exponential-backoff retry and dead-letter routing.
// HttpRequestException is transient: the server may recover; retry with backoff.
// McpException and GuardrailRejectedException are permanent: skip retries, route to dead letter.
// Dead-letter entries carry the failure context for operator review and resubmission.

using TravelBooking.Orchestration;
using TravelBooking.Orchestration.Guardrails;
using ModelContextProtocol;

namespace TravelBooking.Blazor.Services;

public sealed class RetryJobProcessor(
    McpJobQueue queue,
    McpJobQueue deadLetter,
    JobStatusStore statusStore,
    IServiceScopeFactory scopeFactory,
    ILogger<RetryJobProcessor> logger)
    : BackgroundService
{
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.DequeueAllAsync(stoppingToken))
        {
            statusStore.Update(job.Id, JobState.Running);
            await ProcessWithRetryAsync(job, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(McpJob job, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var result = await scope.ServiceProvider
                    .GetRequiredService<ReActOrchestrator>()
                    .RunAsync(job.UserMessage, ct);

                statusStore.Update(job.Id, JobState.Completed, result);
                return;
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.LogWarning(
                    "Job {JobId} transient failure on attempt {N}, retrying in {Delay}s",
                    job.Id, attempt, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex) when (ex is McpException or GuardrailRejectedException or HttpRequestException)
            {
                logger.LogError(ex,
                    "Job {JobId} permanent failure on attempt {N}, routing to dead letter",
                    job.Id, attempt);
                statusStore.Update(job.Id, JobState.Failed, ex.Message);
                // Annotate the dead-letter entry with the failure context.
                await deadLetter.EnqueueAsync(
                    job with
                    {
                        UserMessage = $"[attempt {attempt} failed: {ex.Message}] {job.UserMessage}"
                    }, ct);
                return;
            }
        }

        statusStore.Update(job.Id, JobState.Failed,
            $"Exhausted {MaxRetries} retry attempts.");
    }
}
