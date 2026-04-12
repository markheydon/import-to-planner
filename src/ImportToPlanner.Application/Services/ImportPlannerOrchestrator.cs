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
            request.ContainerId,
            request.PlanName,
            cancellationToken);

        var planAction = existingPlan is null ? PlannedEntityAction.Create : PlannedEntityAction.Reuse;
        var existingBuckets = existingPlan is null
            ? []
            : await plannerGateway.GetBucketsAsync(existingPlan.Id, cancellationToken);

        var existingTasks = existingPlan is null
            ? []
            : await plannerGateway.GetTasksAsync(existingPlan.Id, cancellationToken);

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
                    ResolveGoals(row.Goal),
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
                    ResolveGoals(row.Goal),
                    PlannedEntityAction.Skip,
                    "Task already exists in target plan."));

                continue;
            }

            taskActions.Add(new ImportTaskPlanItem(
                row.RowNumber,
                row.TaskName,
                resolvedBucket,
                ResolveGoals(row.Goal),
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

        return new ImportPlanPreview
        {
            ContainerId = request.ContainerId,
            PlanName = request.PlanName,
            PlanAction = planAction,
            BucketActions = bucketActions,
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

        if (!string.Equals(request.ContainerId, preview.ContainerId, StringComparison.Ordinal) ||
            !string.Equals(request.PlanName, preview.PlanName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Preview does not match request.");
        }

        var created = new List<string>();
        var reusedOrSkipped = new List<string>();
        var errors = new List<string>();
        var manualActions = new List<ManualAction>();

        PlannerPlan plan;
        try
        {
            plan = preview.PlanAction == PlannedEntityAction.Create
                ? await plannerGateway.CreatePlanAsync(request.ContainerId, request.PlanName, cancellationToken)
                : (await plannerGateway.FindPlanByNameAsync(request.ContainerId, request.PlanName, cancellationToken))
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
            return new ImportExecutionResult
            {
                Created = created,
                ReusedOrSkipped = reusedOrSkipped,
                Errors = errors,
                ManualActions = manualActions,
            };
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

        var goalsToCreate = preview.TaskActions
            .Where(task => task.Action == PlannedEntityAction.Create && task.Goals is not null)
            .SelectMany(task => task.Goals!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(goal => goal, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var goal in goalsToCreate)
        {
            manualActions.Add(new ManualAction(
                "CreateGoal",
                goal,
                null,
                "Create this goal manually in Planner, then link imported tasks to it."));
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

                foreach (var goal in taskAction.Goals ?? [])
                {
                    manualActions.Add(new ManualAction(
                        "LinkTaskToGoal",
                        goal,
                        sourceRow.TaskName,
                        "Link this imported task to the goal manually in Planner."));
                }
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
            ManualActions = manualActions,
        };
    }

    private static string ResolveDefaultBucketName()
    {
        return DefaultBucketName;
    }

    private static void ValidateRequest(ImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ContainerId))
        {
            throw new ArgumentException("Container is required.", nameof(request));
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

    private static IReadOnlyList<string>? ResolveGoals(string? goal)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            return null;
        }

        return [goal.Trim()];
    }
}
