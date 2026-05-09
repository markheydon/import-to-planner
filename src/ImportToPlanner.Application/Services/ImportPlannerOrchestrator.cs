using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Generates and executes idempotent Planner import actions.
/// </summary>
public sealed class ImportPlannerOrchestrator(IPlannerGateway plannerGateway) : IImportPlannerOrchestrator
{
    private const string DefaultBucketName = "General";
    private const string TaskAlreadyExistsReason = "already exists";

    /// <inheritdoc/>
    public async Task<ImportPlanPreview> BuildPreviewAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var existingPlan = await plannerGateway.GetPlanByIdAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("The selected plan was not found. Refresh plans and select an existing plan.");

        if (!string.Equals(existingPlan.ContainerId, request.ContainerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected plan does not belong to the selected container.");
        }

        var existingBuckets = await plannerGateway.GetBucketsAsync(existingPlan.Id, cancellationToken);
        var existingTasks = await plannerGateway.GetTasksAsync(existingPlan.Id, cancellationToken);

        var bucketLookup = existingBuckets.ToDictionary(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase);
        var taskLookup = existingTasks
            .Select(task => task.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requestFingerprint = BuildRequestFingerprint(request);
        var plannerStateFingerprint = BuildPlannerStateFingerprint(existingBuckets, existingTasks);

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
                    ResolveGoalList(row.Goal),
                    PlannedEntityAction.Skip,
                    "duplicate in CSV",
                    ReportStatus: "Skipped"));

                continue;
            }

            if (taskLookup.Contains(row.TaskName))
            {
                taskActions.Add(new ImportTaskPlanItem(
                    row.RowNumber,
                    row.TaskName,
                    resolvedBucket,
                    ResolveGoalList(row.Goal),
                    PlannedEntityAction.Skip,
                    TaskAlreadyExistsReason,
                    ReportStatus: "Skipped"));

                continue;
            }

            taskActions.Add(new ImportTaskPlanItem(
                row.RowNumber,
                row.TaskName,
                resolvedBucket,
                ResolveGoalList(row.Goal),
                PlannedEntityAction.Create,
                ReportStatus: "Pending"));
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
            PlanName = existingPlan.Title,
            PlanId = existingPlan.Id,
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = false,
            RequestFingerprint = requestFingerprint,
            PlannerStateFingerprint = plannerStateFingerprint,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
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
            !string.Equals(request.PlanId, preview.PlanId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Preview does not match request.");
        }

        if (preview.HasValidationErrors)
        {
            throw new InvalidOperationException("Execution is blocked because validation errors are unresolved.");
        }

        if (!string.Equals(BuildRequestFingerprint(request), preview.RequestFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Request data changed after preview. Generate a fresh preview before execution.");
        }

        var stalePreviewReason = await VerifyPreviewFreshnessAsync(request, preview, cancellationToken);
        if (stalePreviewReason is not null)
        {
            throw new InvalidOperationException(stalePreviewReason);
        }

        var created = new List<string>();
        var reusedOrSkipped = new List<string>();
        var errors = new List<string>();
        var manualActions = new List<ManualAction>();
        var emittedGoalTaskLinks = new HashSet<(string Goal, string TaskName)>(GoalTaskLinkComparer.Instance);

        PlannerPlan plan;
        try
        {
            plan = await plannerGateway.GetPlanByIdAsync(request.PlanId, cancellationToken)
                ?? throw new InvalidOperationException("Selected plan was not found.");

            reusedOrSkipped.Add($"Plan: {plan.Title} (reused)");
        }
        catch (Exception ex)
        {
            errors.Add($"Plan operation failed: {PlannerGraphErrorMapper.ToUserSafeMessage(ex, "Unable to load selected plan.")}");
            return new ImportExecutionResult
            {
                PlanId = request.PlanId,
                Created = created,
                ReusedOrSkipped = reusedOrSkipped,
                Errors = errors,
                ManualActions = manualActions,
                OutcomeSummary = BuildOutcomeSummary(created, reusedOrSkipped, errors, manualActions),
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
                errors.Add($"Bucket '{bucketAction.Key}' failed: {PlannerGraphErrorMapper.ToUserSafeMessage(ex, "Bucket operation failed.")}");
            }
        }

        var goalsToCreate = preview.TaskActions
            .Where(task => task.Action != PlannedEntityAction.Skip ||
                           IsTaskAlreadyExistsReason(task.Reason))
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
                "Verify this goal/category exists in Planner, create it if needed, then link imported tasks to it."));
        }

        var rowsByNumber = request.Rows.ToDictionary(row => row.RowNumber);

        foreach (var taskAction in preview.TaskActions)
        {
            if (taskAction.Action != PlannedEntityAction.Create)
            {
                reusedOrSkipped.Add($"Task: {taskAction.TaskName} ({taskAction.Reason ?? "skipped"})");

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
                                "Link this existing task to the goal manually in Planner."));
                        }
                    }
                }

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
                    if (emittedGoalTaskLinks.Add((goal, sourceRow.TaskName)))
                    {
                        manualActions.Add(new ManualAction(
                            "LinkTaskToGoal",
                            goal,
                            sourceRow.TaskName,
                            "Link this imported task to the goal manually in Planner."));
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Task '{taskAction.TaskName}' failed: {PlannerGraphErrorMapper.ToUserSafeMessage(ex, "Task operation failed.")}");
            }
        }

        return new ImportExecutionResult
        {
            PlanId = plan.Id,
            Created = created,
            ReusedOrSkipped = reusedOrSkipped,
            Errors = errors,
            ManualActions = manualActions,
            OutcomeSummary = BuildOutcomeSummary(created, reusedOrSkipped, errors, manualActions),
        };
    }

    private async Task<string?> VerifyPreviewFreshnessAsync(ImportRequest request, ImportPlanPreview preview, CancellationToken cancellationToken)
    {
        var liveBuckets = await plannerGateway.GetBucketsAsync(request.PlanId, cancellationToken);
        var liveTasks = await plannerGateway.GetTasksAsync(request.PlanId, cancellationToken);
        var liveStateFingerprint = BuildPlannerStateFingerprint(liveBuckets, liveTasks);

        if (!string.Equals(liveStateFingerprint, preview.PlannerStateFingerprint, StringComparison.Ordinal))
        {
            return "Planner state changed after preview. Run a fresh preview before execution.";
        }

        return null;
    }

    private static ImportExecutionOutcomeSummary BuildOutcomeSummary(
        List<string> created,
        List<string> reusedOrSkipped,
        List<string> errors,
        List<ManualAction> manualActions)
    {
        var hasSuccessfulActions = created.Count > 0 || reusedOrSkipped.Count > 0;
        var hasErrors = errors.Count > 0;

        return new ImportExecutionOutcomeSummary(
            created.Count,
            reusedOrSkipped.Count,
            errors.Count,
            manualActions.Count,
            hasSuccessfulActions && hasErrors);
    }

    private static string BuildRequestFingerprint(ImportRequest request)
    {
        var lines = new List<string>
        {
            request.ContainerId,
            request.PlanId,
            request.PlanName,
            request.ContainerType.ToString(),
        };

        lines.AddRange(request.Rows
            .OrderBy(row => row.RowNumber)
            .Select(row => string.Join("|",
                row.RowNumber,
                row.TaskName.Trim(),
                row.Description?.Trim() ?? string.Empty,
                row.Priority?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                row.Bucket?.Trim() ?? string.Empty,
                row.Goal?.Trim() ?? string.Empty)));

        return ComputeFingerprint(string.Join("\n", lines));
    }

    private static string BuildPlannerStateFingerprint(
        IReadOnlyCollection<PlannerBucket> buckets,
        IReadOnlyCollection<PlannerTaskSnapshot> tasks)
    {
        var stateLines = buckets
            .Select(bucket => $"B:{bucket.Name.Trim()}")
            .Concat(tasks.Select(task => $"T:{task.Title.Trim()}"))
            .OrderBy(line => line, StringComparer.OrdinalIgnoreCase);

        return ComputeFingerprint(string.Join("\n", stateLines));
    }

    private static string ComputeFingerprint(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
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

        if (string.IsNullOrWhiteSpace(request.PlanId))
        {
            throw new ArgumentException("Plan is required.", nameof(request));
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

    private static IReadOnlyList<string>? ResolveGoalList(string? goal)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            return null;
        }

        return [goal.Trim()];
    }

    private static bool IsTaskAlreadyExistsReason(string? reason)
    {
        return string.Equals(reason, TaskAlreadyExistsReason, StringComparison.OrdinalIgnoreCase);
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
