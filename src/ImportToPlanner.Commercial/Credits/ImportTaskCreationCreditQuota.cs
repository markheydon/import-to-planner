using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Commercial credit quota implementation for import task creates.
/// </summary>
public sealed class ImportTaskCreationCreditQuota(
    IEnsureCurrentCreditBalanceUseCase ensureCurrentCreditBalanceUseCase,
    ICreditLedgerStore ledgerStore,
    ImportExecutionCreditBalanceCache executionCreditBalanceCache) : IImportTaskCreationQuota
{
    /// <inheritdoc/>
    public async Task<TaskCreationQuotaResult> BeforeCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var remaining = await GetOrEnsureRemainingCreditsAsync(context, cancellationToken).ConfigureAwait(false);
        if (remaining.Status == TaskCreationQuotaStatus.Unavailable)
        {
            return new TaskCreationQuotaResult(
                TaskCreationQuotaStatus.Unavailable,
                remaining.DiagnosticCode);
        }

        if ((remaining.RemainingCredits ?? 0) <= 0)
        {
            return new TaskCreationQuotaResult(
                TaskCreationQuotaStatus.Exhausted,
                CommercialCreditFailureCodes.Exhausted,
                remaining.RemainingCredits);
        }

        return new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: remaining.RemainingCredits);
    }

    /// <inheritdoc/>
    public async Task<TaskCreationQuotaRecordResult> RecordSuccessfulCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.CreatedPlannerTaskId))
        {
            return new TaskCreationQuotaRecordResult(
                false,
                CommercialCreditFailureCodes.UsageRecordFailed);
        }

        var firstAttempt = await RecordUsageOnceAsync(context, cancellationToken).ConfigureAwait(false);
        if (firstAttempt is RecordCreditUsageOutcome.Success success)
        {
            executionCreditBalanceCache.SetRemaining(context.TenantId, context.OccurredUtc, success.RemainingCredits);
            return new TaskCreationQuotaRecordResult(true, RemainingCredits: success.RemainingCredits);
        }

        var secondAttempt = await RecordUsageOnceAsync(context, cancellationToken).ConfigureAwait(false);
        if (secondAttempt is RecordCreditUsageOutcome.Success retrySuccess)
        {
            executionCreditBalanceCache.SetRemaining(context.TenantId, context.OccurredUtc, retrySuccess.RemainingCredits);
            return new TaskCreationQuotaRecordResult(true, RemainingCredits: retrySuccess.RemainingCredits);
        }

        if (secondAttempt is RecordCreditUsageOutcome.Failure failure)
        {
            return new TaskCreationQuotaRecordResult(false, failure.FailureCode);
        }

        return new TaskCreationQuotaRecordResult(false, CommercialCreditFailureCodes.UsageRecordFailed);
    }

    private async Task<RemainingCreditsLookup> GetOrEnsureRemainingCreditsAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        if (executionCreditBalanceCache.TryGetRemaining(context.TenantId, context.OccurredUtc, out var cachedRemaining))
        {
            return new RemainingCreditsLookup(cachedRemaining, null);
        }

        var ensureOutcome = await ensureCurrentCreditBalanceUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                context.TenantId,
                context.ActorUserId,
                context.OccurredUtc,
                EnsureBalanceReason.Execute),
            cancellationToken).ConfigureAwait(false);

        if (ensureOutcome is EnsureCurrentCreditBalanceOutcome.Failed failure)
        {
            return new RemainingCreditsLookup(null, failure.Failure.FailureCode);
        }

        var remaining = ((EnsureCurrentCreditBalanceOutcome.Succeeded)ensureOutcome).Result.RemainingCredits;
        executionCreditBalanceCache.SetRemaining(context.TenantId, context.OccurredUtc, remaining);
        return new RemainingCreditsLookup(remaining, null);
    }

    private readonly record struct RemainingCreditsLookup(int? RemainingCredits, string? DiagnosticCode)
    {
        public TaskCreationQuotaStatus Status => DiagnosticCode is null
            ? TaskCreationQuotaStatus.Allow
            : TaskCreationQuotaStatus.Unavailable;
    }

    private Task<RecordCreditUsageOutcome> RecordUsageOnceAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
        => ledgerStore.RecordUsageAsync(
            new RecordCreditUsageRequest(
                context.TenantId,
                context.ActorUserId,
                context.OccurredUtc,
                context.ImportRunId,
                context.CreatedPlannerTaskId!),
            cancellationToken);
}
