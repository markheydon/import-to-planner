using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Executes approved import previews.
/// </summary>
public sealed class ImportExecutionUseCase(
    IPlannerGateway plannerGateway,
    IImportTaskCreationQuota taskCreationQuota) : IImportExecutionUseCase
{
    private const string CreditExhaustedDiagnosticCode = "credits.exhausted";
    private const string CreditUnavailableDiagnosticCode = "credits.ledger_unavailable";
    private const string CreditBalanceReportUnavailableDiagnosticCode = "credits.balance_report_unavailable";
    private const string CreditUsageRecordFailedDiagnosticCode = "credits.usage_record_failed";
    private const string CreditExhaustedMessage = "Import stopped because your organisation has no credits remaining for new tasks.";
    private const string CreditUnavailableMessage = "Import could not continue because credit balance is unavailable.";
    private const string CreditBalanceReportUnavailableMessage = "Remaining credits could not be loaded for this execution report.";
    private const string CreditUsageRecordFailedMessage = "Import stopped because a credit usage record could not be saved after a task was created.";

    /// <inheritdoc/>
    public async Task HandleAsync(
        ImportExecutionRequest request,
        IImportExecutionOutputBoundary outputBoundary,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(outputBoundary);

        var planningRequest = request.Request;
        var preview = request.ApprovedPreview;

        if (!string.Equals(planningRequest.ContainerId, preview.ContainerId, StringComparison.Ordinal) ||
            !string.Equals(planningRequest.PlanId, preview.PlanId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Preview does not match request.");
        }

        if (preview.HasValidationErrors)
        {
            throw new InvalidOperationException("Execution is blocked because validation errors are unresolved.");
        }

        if (!string.Equals(ImportFingerprintBuilder.BuildRequestFingerprint(planningRequest), preview.RequestFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Request data changed after preview. Generate a fresh preview before execution.");
        }

        var stalePreviewReason = await VerifyPreviewFreshnessAsync(planningRequest, preview, cancellationToken);
        if (stalePreviewReason is not null)
        {
            throw new StaleImportPreviewException(stalePreviewReason);
        }

        var created = new List<ImportExecutionItem>();
        var reusedOrSkipped = new List<ImportExecutionItem>();
        var failures = new List<PlannerOperationFailure>();
        var manualActions = new List<ManualAction>();
        var emittedGoalTaskLinks = new HashSet<(string Goal, string TaskName)>(GoalTaskLinkComparer.Instance);

        PlannerPlan plan;
        Dictionary<string, PlannerBucket> bucketCache;

        try
        {
            plan = await plannerGateway.GetPlanByIdAsync(planningRequest.PlanId, cancellationToken)
                ?? throw new InvalidOperationException("Selected plan was not found.");
            reusedOrSkipped.Add(new ImportExecutionItem(PlannerFailureTarget.Plan, plan.Title, plan.Id));

            bucketCache = (await plannerGateway.GetBucketsAsync(plan.Id, cancellationToken))
                .ToDictionary(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch (PlannerOperationException ex)
        {
            failures.Add(CreateBoundaryFailure(ex, planningRequest.PlanId));
            await PresentFailureOnlyResultAsync(planningRequest.PlanId, outputBoundary, failures, cancellationToken);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures.Add(CreateUnexpectedFailure(
                PlannerFailureTarget.Plan,
                planningRequest.PlanId,
                "UnexpectedPlanLookupFailure",
                ex));
            await PresentFailureOnlyResultAsync(planningRequest.PlanId, outputBoundary, failures, cancellationToken);
            return;
        }

        foreach (var bucketAction in preview.BucketActions)
        {
            if (bucketAction.Value == PlannedEntityAction.Reuse)
            {
                reusedOrSkipped.Add(new ImportExecutionItem(PlannerFailureTarget.Bucket, bucketAction.Key));
                continue;
            }

            try
            {
                var createdBucket = await plannerGateway.CreateBucketAsync(plan.Id, bucketAction.Key, cancellationToken);
                bucketCache[createdBucket.Name] = createdBucket;
                created.Add(new ImportExecutionItem(PlannerFailureTarget.Bucket, createdBucket.Name, createdBucket.Id));
            }
            catch (PlannerOperationException ex)
            {
                failures.Add(ex.Failure with { Target = PlannerFailureTarget.Bucket, Reference = bucketAction.Key });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(CreateUnexpectedFailure(
                    PlannerFailureTarget.Bucket,
                    bucketAction.Key,
                    "UnexpectedBucketFailure",
                    ex));
            }
        }

        var goalsToCreate = preview.TaskActions
            .Where(task => task.Action != PlannedEntityAction.Skip || IsTaskAlreadyExistsReason(task.Reason))
            .Where(task => task.Goals is not null)
            .SelectMany(task => task.Goals!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(goal => goal, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var goal in goalsToCreate)
        {
            manualActions.Add(new ManualAction(
                "EnsureGoalExists",
                goal,
                null,
                null));
        }

        var rowsByNumber = planningRequest.Rows.ToDictionary(row => row.RowNumber);
        var importRunId = Guid.NewGuid().ToString("N");
        var creditsUsed = 0;
        int? remainingCredits = null;
        var stopFurtherCreates = false;
        string? stopDiagnosticCode = null;

        foreach (var taskAction in preview.TaskActions)
        {
            if (taskAction.Action != PlannedEntityAction.Create)
            {
                reusedOrSkipped.Add(new ImportExecutionItem(PlannerFailureTarget.Task, taskAction.TaskName));

                if (IsTaskAlreadyExistsReason(taskAction.Reason))
                {
                    foreach (var goal in taskAction.Goals ?? [])
                    {
                        if (emittedGoalTaskLinks.Add((goal, taskAction.TaskName)))
                        {
                            manualActions.Add(new ManualAction(
                                "LinkTaskToGoal",
                                goal,
                                taskAction.TaskName,
                                null));
                        }
                    }
                }

                continue;
            }

            if (stopFurtherCreates)
            {
                failures.Add(CreateCreditFailure(
                    taskAction.TaskName,
                    stopDiagnosticCode ?? CreditExhaustedDiagnosticCode,
                    stopDiagnosticCode == CreditUsageRecordFailedDiagnosticCode
                        ? CreditUsageRecordFailedMessage
                        : CreditExhaustedMessage));
                continue;
            }

            if (request.Metering is not null)
            {
                var quotaResult = await taskCreationQuota.BeforeCreateAsync(
                    new ImportTaskCreationQuotaContext(
                        request.Metering.TenantId,
                        request.Metering.ActorUserId,
                        DateTimeOffset.UtcNow,
                        importRunId,
                        taskAction.TaskName),
                    cancellationToken).ConfigureAwait(false);

                remainingCredits = quotaResult.RemainingCredits;
                if (quotaResult.Status == TaskCreationQuotaStatus.Unavailable)
                {
                    failures.Add(CreateCreditFailure(
                        taskAction.TaskName,
                        quotaResult.DiagnosticCode ?? CreditUnavailableDiagnosticCode,
                        CreditUnavailableMessage));
                    stopFurtherCreates = true;
                    stopDiagnosticCode = quotaResult.DiagnosticCode ?? CreditUnavailableDiagnosticCode;
                    continue;
                }

                if (quotaResult.Status == TaskCreationQuotaStatus.Exhausted)
                {
                    failures.Add(CreateCreditFailure(
                        taskAction.TaskName,
                        CreditExhaustedDiagnosticCode,
                        CreditExhaustedMessage));
                    stopFurtherCreates = true;
                    stopDiagnosticCode = CreditExhaustedDiagnosticCode;
                    continue;
                }
            }

            if (!bucketCache.TryGetValue(taskAction.Bucket, out var bucket))
            {
                failures.Add(new PlannerOperationFailure(
                    PlannerFailureCategory.Validation,
                    PlannerFailureTarget.Task,
                    taskAction.TaskName,
                    $"Task '{taskAction.TaskName}' failed because bucket '{taskAction.Bucket}' is unavailable.",
                    false,
                    "BucketUnavailable"));
                continue;
            }

            try
            {
                var sourceRow = rowsByNumber[taskAction.RowNumber];
                var createdTask = await plannerGateway.CreateTaskAsync(
                    plan.Id,
                    bucket.Id,
                    sourceRow.TaskName,
                    sourceRow.Description,
                    sourceRow.Priority,
                    sourceRow.Goal,
                    cancellationToken);

                created.Add(new ImportExecutionItem(PlannerFailureTarget.Task, createdTask.Title, createdTask.Id));

                if (request.Metering is not null)
                {
                    var recordResult = await taskCreationQuota.RecordSuccessfulCreateAsync(
                        new ImportTaskCreationQuotaContext(
                            request.Metering.TenantId,
                            request.Metering.ActorUserId,
                            DateTimeOffset.UtcNow,
                            importRunId,
                            taskAction.TaskName,
                            createdTask.Id),
                        cancellationToken).ConfigureAwait(false);

                    if (!recordResult.Succeeded)
                    {
                        // Planner task is intentionally retained when usage recording fails; credits may need manual reconciliation.
                        failures.Add(CreateCreditFailure(
                            taskAction.TaskName,
                            recordResult.DiagnosticCode ?? CreditUsageRecordFailedDiagnosticCode,
                            CreditUsageRecordFailedMessage));
                        stopFurtherCreates = true;
                        stopDiagnosticCode = recordResult.DiagnosticCode ?? CreditUsageRecordFailedDiagnosticCode;
                    }
                    else
                    {
                        creditsUsed++;
                        remainingCredits = recordResult.RemainingCredits;
                    }
                }

                foreach (var goal in taskAction.Goals ?? [])
                {
                    if (emittedGoalTaskLinks.Add((goal, sourceRow.TaskName)))
                    {
                        manualActions.Add(new ManualAction(
                            "LinkTaskToGoal",
                            goal,
                            sourceRow.TaskName,
                            null));
                    }
                }
            }
            catch (PlannerOperationException ex)
            {
                failures.Add(ex.Failure with { Target = PlannerFailureTarget.Task, Reference = taskAction.TaskName });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(CreateUnexpectedFailure(
                    PlannerFailureTarget.Task,
                    taskAction.TaskName,
                    "UnexpectedTaskFailure",
                    ex));
            }
        }

        if (request.Metering is not null && remainingCredits is null)
        {
            var balanceResult = await taskCreationQuota.BeforeCreateAsync(
                new ImportTaskCreationQuotaContext(
                    request.Metering.TenantId,
                    request.Metering.ActorUserId,
                    DateTimeOffset.UtcNow,
                    importRunId,
                    string.Empty),
                cancellationToken).ConfigureAwait(false);

            if (balanceResult.Status == TaskCreationQuotaStatus.Unavailable)
            {
                failures.Add(CreateCreditBalanceReportFailure());
            }
            else
            {
                remainingCredits = balanceResult.RemainingCredits;
            }
        }

        var outcomeSummary = BuildOutcomeSummary(created, reusedOrSkipped, failures, manualActions);
        var response = new ImportExecutionResult
        {
            PlanId = plan.Id,
            CreatedItems = created,
            ReusedOrSkippedItems = reusedOrSkipped,
            FailureItems = failures,
            ManualActions = manualActions,
            OutcomeSummary = outcomeSummary,
            CreditsUsed = request.Metering is null ? null : creditsUsed,
            RemainingCredits = request.Metering is null ? null : remainingCredits,
        };

        await outputBoundary.PresentAsync(response, cancellationToken);
    }

    private static async Task PresentFailureOnlyResultAsync(
        string planId,
        IImportExecutionOutputBoundary outputBoundary,
        List<PlannerOperationFailure> failures,
        CancellationToken cancellationToken)
    {
        var emptyItems = new List<ImportExecutionItem>();
        var manualActions = new List<ManualAction>();
        var response = new ImportExecutionResult
        {
            PlanId = planId,
            CreatedItems = emptyItems,
            ReusedOrSkippedItems = emptyItems,
            FailureItems = failures,
            ManualActions = manualActions,
            OutcomeSummary = BuildOutcomeSummary(emptyItems, emptyItems, failures, manualActions),
        };

        await outputBoundary.PresentAsync(response, cancellationToken);
    }

    private static ImportExecutionOutcomeSummary BuildOutcomeSummary(
        List<ImportExecutionItem> created,
        List<ImportExecutionItem> reusedOrSkipped,
        List<PlannerOperationFailure> failures,
        List<ManualAction> manualActions)
    {
        var hasSuccessfulActions = created.Count > 0 || reusedOrSkipped.Count > 0;
        var hasErrors = failures.Count > 0;

        return new ImportExecutionOutcomeSummary(
            created.Count,
            reusedOrSkipped.Count,
            failures.Count,
            manualActions.Count,
            IsPartialSuccess: hasSuccessfulActions && hasErrors,
            IsFullFailure: !hasSuccessfulActions && hasErrors);
    }

    private async Task<string?> VerifyPreviewFreshnessAsync(
        ImportPlanningRequest request,
        ImportPlanPreview preview,
        CancellationToken cancellationToken)
    {
        var liveBuckets = await plannerGateway.GetBucketsAsync(request.PlanId, cancellationToken);
        var liveTasks = await plannerGateway.GetTasksAsync(request.PlanId, cancellationToken);
        var liveStateFingerprint = ImportFingerprintBuilder.BuildPlannerStateFingerprint(liveBuckets, liveTasks);

        if (!string.Equals(liveStateFingerprint, preview.PlannerStateFingerprint, StringComparison.Ordinal))
        {
            return "Planner state changed after preview. Run a fresh preview before execution.";
        }

        return null;
    }

    private static bool IsTaskAlreadyExistsReason(string? reason)
    {
        return string.Equals(reason, "already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static PlannerOperationFailure CreateCreditFailure(
        string taskName,
        string diagnosticCode,
        string message)
        => new(
            PlannerFailureCategory.Validation,
            PlannerFailureTarget.Task,
            taskName,
            message,
            Retryable: false,
            diagnosticCode);

    private static PlannerOperationFailure CreateCreditBalanceReportFailure()
        => new(
            PlannerFailureCategory.Unavailable,
            PlannerFailureTarget.Workflow,
            null,
            CreditBalanceReportUnavailableMessage,
            Retryable: false,
            CreditBalanceReportUnavailableDiagnosticCode);

    private static PlannerOperationFailure CreateUnexpectedFailure(
        PlannerFailureTarget target,
        string? reference,
        string diagnosticCode,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new PlannerOperationFailure(
            PlannerFailureCategory.Unknown,
            target,
            reference,
            exception.Message,
            false,
            diagnosticCode);
    }

    private static PlannerOperationFailure CreateBoundaryFailure(
        PlannerOperationException exception,
        string planId)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var target = exception.Failure.Target is PlannerFailureTarget.Workflow or PlannerFailureTarget.Plan
            ? exception.Failure.Target
            : PlannerFailureTarget.Plan;

        return exception.Failure with
        {
            Target = target,
            Reference = exception.Failure.Reference ?? planId,
        };
    }

    private sealed class GoalTaskLinkComparer : IEqualityComparer<(string Goal, string TaskName)>
    {
        public static GoalTaskLinkComparer Instance { get; } = new();

        public bool Equals((string Goal, string TaskName) x, (string Goal, string TaskName) y)
        {
            return string.Equals(x.Goal, y.Goal, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TaskName, y.TaskName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Goal, string TaskName) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Goal),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TaskName));
        }
    }
}
