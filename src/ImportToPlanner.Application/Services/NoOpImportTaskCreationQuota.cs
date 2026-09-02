using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// No-op quota used when commercial mode is disabled.
/// </summary>
public sealed class NoOpImportTaskCreationQuota : IImportTaskCreationQuota
{
    /// <inheritdoc/>
    public Task<TaskCreationQuotaResult> BeforeCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow));

    /// <inheritdoc/>
    public Task<TaskCreationQuotaRecordResult> RecordSuccessfulCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(new TaskCreationQuotaRecordResult(Succeeded: true));
}
