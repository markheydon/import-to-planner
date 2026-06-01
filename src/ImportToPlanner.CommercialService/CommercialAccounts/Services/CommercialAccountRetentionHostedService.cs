using ImportToPlanner.CommercialService.CommercialAccounts.Services;

namespace ImportToPlanner.CommercialService;

/// <summary>
/// Runs scheduled retention sweeps for hosted commercial accounts and audit records.
/// </summary>
internal sealed class CommercialAccountRetentionHostedService(
    CommercialProfileService commercialProfileService,
    CommercialModeOptions commercialModeOptions,
    ILogger<CommercialAccountRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    private const int DefaultBatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!commercialModeOptions.Enabled || !commercialModeOptions.RetentionSweepEnabled)
        {
            logger.LogInformation("Commercial retention sweep hosted service is disabled by configuration.");
            return;
        }

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
