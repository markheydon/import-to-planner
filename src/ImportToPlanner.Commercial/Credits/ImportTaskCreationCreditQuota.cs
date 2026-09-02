using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Commercial credit quota implementation for import task creates.
/// </summary>
public sealed class ImportTaskCreationCreditQuota(
    IEnsureCurrentCreditBalanceUseCase ensureCurrentCreditBalanceUseCase,
    ICreditLedgerStore ledgerStore) : IImportTaskCreationQuota
{
    /// <inheritdoc/>
    public async Task<TaskCreationQuotaResult> BeforeCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var ensureOutcome = await ensureCurrentCreditBalanceUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                context.TenantId,
                context.ActorUserId,
                context.OccurredUtc,
                EnsureBalanceReason.Execute),
            cancellationToken).ConfigureAwait(false);

        if (ensureOutcome is EnsureCurrentCreditBalanceOutcome.Failed failure)
        {
            return new TaskCreationQuotaResult(
                TaskCreationQuotaStatus.Unavailable,
                failure.Failure.FailureCode);
        }

        var remaining = ((EnsureCurrentCreditBalanceOutcome.Succeeded)ensureOutcome).Result.RemainingCredits;
        if (remaining <= 0)
        {
            return new TaskCreationQuotaResult(
                TaskCreationQuotaStatus.Exhausted,
                CommercialCreditFailureCodes.Exhausted,
                remaining);
        }

        return new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: remaining);
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
            return new TaskCreationQuotaRecordResult(true, RemainingCredits: success.RemainingCredits);
        }

        var secondAttempt = await RecordUsageOnceAsync(context, cancellationToken).ConfigureAwait(false);
        return secondAttempt switch
        {
            RecordCreditUsageOutcome.Success retrySuccess => new TaskCreationQuotaRecordResult(
                true,
                RemainingCredits: retrySuccess.RemainingCredits),
            RecordCreditUsageOutcome.Failure failure => new TaskCreationQuotaRecordResult(
                false,
                failure.FailureCode),
            _ => new TaskCreationQuotaRecordResult(false, CommercialCreditFailureCodes.UsageRecordFailed),
        };
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
