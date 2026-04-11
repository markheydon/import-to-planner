using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Generates and executes idempotent Planner import actions.
/// </summary>
public sealed class ImportPlannerOrchestrator(IPlannerGateway plannerGateway) : IImportPlannerOrchestrator
{
    private const string DefaultBucketName = "General";

    /// <inheritdoc/>
    public async Task<ImportPlanPreview> BuildPreviewAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var existingPlan = await plannerGateway.FindPlanByNameAsync(
            request.GroupId,
            request.PlanName,
            cancellationToken);

        var planAction = existingPlan is null ? PlannedEntityAction.Create : PlannedEntityAction.Reuse;
        var existingBuckets = existingPlan is null
            ? []
            : await plannerGateway.GetBucketsAsync(existingPlan.Id, cancellationToken);

        var existingTasks = existingPlan is null
            ? []
            : await plannerGateway.GetTasksAsync(existingPlan.Id, cancellationToken);

        var existingGoals = existingPlan is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : await plannerGateway.GetGoalsAsync(existingPlan.Id, cancellationToken);

        var bucketLookup = existingBuckets.ToDictionary(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase);
        var taskLookup = existingTasks
            .Select(task => task.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var csvSeenTaskNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var taskActions = new List<ImportTaskPlanItem>();

        foreach (var row in request.Rows)
        {
            var resolvedBucket = string.IsNullOrWhiteSpace(row.Bucket)
                ? ResolveDefaultBucketName()
                : row.Bucket!;

            if (!csvSeenTaskNames.Add(row.TaskName))
            {
                taskActions.Add(new ImportTaskPlanItem(
                    row.RowNumber,
                    row.TaskName,
                    resolvedBucket,
                    row.Goal,
                    PlannedEntityAction.Skip,
                    "Duplicate task name in CSV."));

                continue;
            }

            if (taskLookup.Contains(row.TaskName))
            {
                taskActions.Add(new ImportTaskPlanItem(
                    row.RowNumber,
                    row.TaskName,
                    resolvedBucket,
                    row.Goal,
                    PlannedEntityAction.Skip,
                    "Task already exists in target plan."));

                continue;
            }

            taskActions.Add(new ImportTaskPlanItem(
                row.RowNumber,
                row.TaskName,
                resolvedBucket,
                row.Goal,
                PlannedEntityAction.Create));
        }

        var requiredBuckets = taskActions
            .Where(task => task.Action == PlannedEntityAction.Create)
            .Select(task => task.Bucket)
            .Where(bucket => !string.IsNullOrWhiteSpace(bucket))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(bucket => bucket, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var bucketActions = requiredBuckets.ToDictionary(
            bucket => bucket,
            bucket => bucketLookup.ContainsKey(bucket)
                ? PlannedEntityAction.Reuse
                : PlannedEntityAction.Create,
            StringComparer.OrdinalIgnoreCase);

        var goals = taskActions
            .Where(task => task.Action == PlannedEntityAction.Create)
            .Select(task => task.Goal)
            .Where(goal => !string.IsNullOrWhiteSpace(goal))
            .Select(goal => goal!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(goal => goal, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var goalActions = goals.ToDictionary(
            goal => goal,
            goal => existingGoals.Contains(goal)
                ? PlannedEntityAction.Reuse
                : PlannedEntityAction.Create,
            StringComparer.OrdinalIgnoreCase);

        return new ImportPlanPreview
        {
            GroupId = request.GroupId,
            PlanName = request.PlanName,
            PlanAction = planAction,
            BucketActions = bucketActions,
            GoalActions = goalActions,
            TaskActions = taskActions,
        };
    }

    /// <inheritdoc/>
    public async Task<ImportExecutionResult> ExecuteAsync(
        ImportRequest request,
        ImportPlanPreview preview,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        ArgumentNullException.ThrowIfNull(preview);

        if (!string.Equals(request.GroupId, preview.GroupId, StringComparison.Ordinal) ||
            !string.Equals(request.PlanName, preview.PlanName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Preview does not match request.");
        }

        var created = new List<string>();
        var reusedOrSkipped = new List<string>();
        var errors = new List<string>();

        PlannerPlan plan;
        try
        {
            plan = preview.PlanAction == PlannedEntityAction.Create
                ? await plannerGateway.CreatePlanAsync(request.GroupId, request.PlanName, cancellationToken)
                : (await plannerGateway.FindPlanByNameAsync(request.GroupId, request.PlanName, cancellationToken))
                    ?? throw new InvalidOperationException("Plan was expected to exist but was not found.");

            if (preview.PlanAction == PlannedEntityAction.Create)
            {
                created.Add($"Plan: {request.PlanName}");
            }
            else
            {
                reusedOrSkipped.Add($"Plan: {request.PlanName} (reused)");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Plan operation failed: {ex.Message}");
            return new ImportExecutionResult { Created = created, ReusedOrSkipped = reusedOrSkipped, Errors = errors };
        }

        var bucketCache = (await plannerGateway.GetBucketsAsync(plan.Id, cancellationToken))
            .ToDictionary(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var bucketAction in preview.BucketActions)
        {
            if (bucketAction.Value == PlannedEntityAction.Reuse)
            {
                reusedOrSkipped.Add($"Bucket: {bucketAction.Key} (reused)");
                continue;
            }

            try
            {
                var createdBucket = await plannerGateway.CreateBucketAsync(plan.Id, bucketAction.Key, cancellationToken);
                bucketCache[createdBucket.Name] = createdBucket;
                created.Add($"Bucket: {createdBucket.Name}");
            }
            catch (Exception ex)
            {
                errors.Add($"Bucket '{bucketAction.Key}' failed: {ex.Message}");
            }
        }

        var goalsToCreate = preview.GoalActions
            .Where(goal => goal.Value == PlannedEntityAction.Create)
            .Select(goal => goal.Key)
            .ToList();

        try
        {
            var createdGoals = await plannerGateway.EnsureGoalsAsync(plan.Id, goalsToCreate, cancellationToken);
            foreach (var goal in preview.GoalActions.Keys)
            {
                if (createdGoals.Contains(goal))
                {
                    created.Add($"Goal name: {goal}");
                }
                else
                {
                    reusedOrSkipped.Add($"Goal name: {goal} (reused)");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Goal/category operation failed: {ex.Message}");
        }

        var rowsByNumber = request.Rows.ToDictionary(row => row.RowNumber);

        foreach (var taskAction in preview.TaskActions)
        {
            if (taskAction.Action != PlannedEntityAction.Create)
            {
                reusedOrSkipped.Add($"Task: {taskAction.TaskName} ({taskAction.Reason ?? "skipped"})");
                continue;
            }

            if (!bucketCache.TryGetValue(taskAction.Bucket, out var bucket))
            {
                errors.Add($"Task '{taskAction.TaskName}' failed: bucket '{taskAction.Bucket}' was unavailable.");
                continue;
            }

            try
            {
                var sourceRow = rowsByNumber[taskAction.RowNumber];
                await plannerGateway.CreateTaskAsync(
                    plan.Id,
                    bucket.Id,
                    sourceRow.TaskName,
                    sourceRow.Description,
                    sourceRow.Priority,
                    sourceRow.Goal,
                    cancellationToken);

                created.Add($"Task: {sourceRow.TaskName}");
            }
            catch (Exception ex)
            {
                errors.Add($"Task '{taskAction.TaskName}' failed: {ex.Message}");
            }
        }

        return new ImportExecutionResult
        {
            Created = created,
            ReusedOrSkipped = reusedOrSkipped,
            Errors = errors,
        };
    }

    private static string ResolveDefaultBucketName()
    {
        return DefaultBucketName;
    }

    private static void ValidateRequest(ImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.GroupId))
        {
            throw new ArgumentException("Group is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PlanName))
        {
            throw new ArgumentException("Plan name is required.", nameof(request));
        }

        if (request.Rows.Count == 0)
        {
            throw new ArgumentException("At least one CSV row is required.", nameof(request));
        }
    }
}
