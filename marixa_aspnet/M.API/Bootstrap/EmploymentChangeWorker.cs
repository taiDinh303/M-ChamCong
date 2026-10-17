using M.Contract.Services.Interface;

namespace M.API.Bootstrap;

public sealed class EmploymentChangeWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<EmploymentChangeWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var applied = await scope.ServiceProvider.GetRequiredService<IEmploymentChangeProcessor>().ApplyDueAsync(stoppingToken);
                if (applied > 0) logger.LogInformation("Applied {Count} due employment changes", applied);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Employment effective-date sweep failed; retrying in one minute"); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
