using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImportToPlanner.Functions;

/// <summary>
/// Daily timer adapter for paid credit lot expiry.
/// </summary>
public sealed class PaidCreditExpiryFunction(
    IExpirePaidCreditLotsUseCase expirePaidCreditLotsUseCase,
    IUtcClock clock,
    ILogger<PaidCreditExpiryFunction> logger)
{
    /// <summary>
    /// Runs the paid credit expiry sweep at least once every twenty-four hours.
    /// </summary>
    [Function(nameof(PaidCreditExpiryFunction))]
    public Task Run([TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
        => RunAsync(cancellationToken);

    /// <summary>
    /// Invokes the expiry use case using the injected clock (test seam).
    /// </summary>
    internal Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Starting paid credit expiry sweep.");
        return expirePaidCreditLotsUseCase.ExecuteAsync(clock.UtcNow, cancellationToken);
    }
}

/// <summary>
/// Function host registrations for paid credit expiry.
/// </summary>
public static class PaidCreditExpiryFunctionRegistration
{
    /// <summary>
    /// Registers the paid credit expiry timer function.
    /// </summary>
    public static IServiceCollection AddPaidCreditExpiryFunction(this IServiceCollection services)
    {
        services.AddTransient<PaidCreditExpiryFunction>();
        return services;
    }
}
