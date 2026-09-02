using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.Tests.TestDoubles;

internal sealed class ConfigurableImportTaskCreationQuota : IImportTaskCreationQuota
{
    public int BeforeCreateCallCount { get; private set; }

    public int RecordCallCount { get; private set; }

    public Queue<TaskCreationQuotaResult> BeforeCreateResults { get; } = new();

    public Queue<TaskCreationQuotaRecordResult> RecordResults { get; } = new();

    public Task<TaskCreationQuotaResult> BeforeCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        BeforeCreateCallCount++;
        return Task.FromResult(
            BeforeCreateResults.Count > 0
                ? BeforeCreateResults.Dequeue()
                : new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 1));
    }

    public Task<TaskCreationQuotaRecordResult> RecordSuccessfulCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken)
    {
        RecordCallCount++;
        return Task.FromResult(
            RecordResults.Count > 0
                ? RecordResults.Dequeue()
                : new TaskCreationQuotaRecordResult(true, RemainingCredits: 0));
    }
}
