// Chapter 9 — Section 9.2.2
// BackgroundService that reads jobs from the McpJobQueue and executes each through ReActOrchestrator.
// Each job runs in an isolated IAsyncDisposable scope so scoped services (McpClient, IChatClient)
// are created fresh per job and disposed when the job finishes.
// A failed job's open MCP transport session cannot contaminate the next job's protocol state.

using Microsoft.Extensions.Hosting;
using TravelBooking.Orchestration;

namespace TravelBooking.Blazor.Services;

public sealed class JobProcessorService(
    McpJobQueue queue,
    JobStatusStore statusStore,
    IServiceScopeFactory scopeFactory,
    ILogger<JobProcessorService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.DequeueAllAsync(stoppingToken))
        {
            statusStore.Update(job.Id, JobState.Running);
            logger.LogInformation("Processing job {JobId}", job.Id);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orch = scope.ServiceProvider
                    .GetRequiredService<ReActOrchestrator>();

                var result = await orch.RunAsync(job.UserMessage, stoppingToken);
                statusStore.Update(job.Id, JobState.Completed, result);
                logger.LogInformation("Job {JobId} completed", job.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Job {JobId} failed", job.Id);
                statusStore.Update(job.Id, JobState.Failed, ex.Message);
            }
        }
    }
}
