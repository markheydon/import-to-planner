using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Executes approved import previews.
/// </summary>
public sealed class ImportExecutionUseCase(IPlannerGateway plannerGateway) : IImportExecutionUseCase
{
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

        var plan = await plannerGateway.GetPlanByIdAsync(planningRequest.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Selected plan was not found.");
        reusedOrSkipped.Add(new ImportExecutionItem(PlannerFailureTarget.Plan, plan.Title, plan.Id));

        var bucketCache = (await plannerGateway.GetBucketsAsync(plan.Id, cancellationToken))
            .ToDictionary(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase);

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

        var outcomeSummary = BuildOutcomeSummary(created, reusedOrSkipped, failures, manualActions);
        var response = new ImportExecutionResult
        {
            PlanId = plan.Id,
            CreatedItems = created,
            ReusedOrSkippedItems = reusedOrSkipped,
            FailureItems = failures,
            ManualActions = manualActions,
            OutcomeSummary = outcomeSummary,
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
