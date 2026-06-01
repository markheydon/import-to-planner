using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Runs scheduled retention sweeps for hosted commercial accounts and audit records.
/// </summary>
internal sealed class CommercialAccountRetentionHostedService(
    CommercialProfileService commercialProfileService,
    ILogger<CommercialAccountRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    private const int DefaultBatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Commercial retention sweep hosted service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var purgedAccountCount = await commercialProfileService
                    .PurgeExpiredAsync(DateTimeOffset.UtcNow, DefaultBatchSize, stoppingToken)
                    .ConfigureAwait(false);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Commercial retention sweep completed. Purged {PurgedAccountCount} account record(s).", purgedAccountCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Commercial retention sweep failed.");
            }

            await Task.Delay(SweepInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}
