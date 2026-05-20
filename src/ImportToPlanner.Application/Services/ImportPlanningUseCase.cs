using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Builds import previews from validated planning requests.
/// </summary>
public sealed class ImportPlanningUseCase(
    IPlannerGateway plannerGateway,
    ICurrentTenantContextAccessor currentTenantContextAccessor,
    ITenantOperationalMetadataStore tenantOperationalMetadataStore,
    DeploymentModeConfiguration deploymentModeConfiguration) : IImportPlanningUseCase
{
    private const string DefaultBucketName = "General";
    private const string TaskAlreadyExistsReason = "already exists";

    /// <inheritdoc/>
    public async Task HandleAsync(
        ImportPlanningRequest request,
        IImportPlanningOutputBoundary outputBoundary,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        ArgumentNullException.ThrowIfNull(outputBoundary);

        var consentResolution = await ResolveConsentAsync(cancellationToken).ConfigureAwait(false);
        if (consentResolution.Status is ConsentResolutionStatus.AdminConsentRequired
            or ConsentResolutionStatus.Declined
            or ConsentResolutionStatus.Unavailable)
        {
            throw new InvalidOperationException(ResolveConsentMessage(consentResolution));
        }

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

        var requestFingerprint = ImportFingerprintBuilder.BuildRequestFingerprint(request);
        var plannerStateFingerprint = ImportFingerprintBuilder.BuildPlannerStateFingerprint(existingBuckets, existingTasks);

        var csvSeenTaskNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var taskActions = new List<ImportTaskPlanItem>();

        foreach (var row in request.Rows)
        {
            var resolvedBucket = string.IsNullOrWhiteSpace(row.Bucket)
                ? DefaultBucketName
                : row.Bucket!;

            if (!csvSeenTaskNames.Add(row.TaskName))
            {
                taskActions.Add(new ImportTaskPlanItem(
                    row.RowNumber,
                    row.TaskName,
                    resolvedBucket,
                    ResolveGoalList(row.Goal),
                    PlannedEntityAction.Skip,
                    "duplicate in CSV"));

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
                    TaskAlreadyExistsReason));

                continue;
            }

            taskActions.Add(new ImportTaskPlanItem(
                row.RowNumber,
                row.TaskName,
                resolvedBucket,
                ResolveGoalList(row.Goal),
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

        var response = new ImportPlanPreview
        {
            ContainerId = request.ContainerId,
            PlanName = existingPlan.Title,
            PlanId = existingPlan.Id,
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = false,
            ValidationFindings = [],
            RequestFingerprint = requestFingerprint,
            PlannerStateFingerprint = plannerStateFingerprint,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = bucketActions,
            TaskActions = taskActions,
        };

        await outputBoundary.PresentAsync(response, cancellationToken);
    }

    private async Task<ConsentResolution> ResolveConsentAsync(CancellationToken cancellationToken)
    {
        if (deploymentModeConfiguration.Mode != DeploymentMode.HostedSharedMultiTenant
            || !deploymentModeConfiguration.UseGraphGateway)
        {
            return ConsentResolution.Granted(deploymentModeConfiguration.RequiredScopes);
        }

        var tenantContext = currentTenantContextAccessor.GetRequiredContext();
        var metadata = await tenantOperationalMetadataStore.GetAsync(tenantContext.TenantId, cancellationToken).ConfigureAwait(false);

        if (metadata is null)
        {
            return new ConsentResolution(
                ConsentResolutionStatus.UserConsentAvailable,
                deploymentModeConfiguration.RequiredScopes,
                deploymentModeConfiguration.AdminConsentUri,
                "consent.user-consent-available");
        }

        return metadata.ConsentStatus switch
        {
            ConsentResolutionStatus.Granted => ConsentResolution.Granted(deploymentModeConfiguration.RequiredScopes),
            ConsentResolutionStatus.UserConsentAvailable => new ConsentResolution(
                ConsentResolutionStatus.UserConsentAvailable,
                deploymentModeConfiguration.RequiredScopes,
                deploymentModeConfiguration.AdminConsentUri,
                "consent.user-consent-available"),
            ConsentResolutionStatus.AdminConsentRequired => new ConsentResolution(
                ConsentResolutionStatus.AdminConsentRequired,
                deploymentModeConfiguration.RequiredScopes,
                deploymentModeConfiguration.AdminConsentUri,
                "consent.admin-consent-required",
                metadata.LastSupportDiagnosticCode),
            ConsentResolutionStatus.Declined => new ConsentResolution(
                ConsentResolutionStatus.Declined,
                deploymentModeConfiguration.RequiredScopes,
                deploymentModeConfiguration.AdminConsentUri,
                "consent.declined",
                metadata.LastSupportDiagnosticCode),
            _ => new ConsentResolution(
                ConsentResolutionStatus.Unavailable,
                deploymentModeConfiguration.RequiredScopes,
                deploymentModeConfiguration.AdminConsentUri,
                "consent.unavailable",
                metadata.LastSupportDiagnosticCode),
        };
    }

    private static string ResolveConsentMessage(ConsentResolution consentResolution)
    {
        ArgumentNullException.ThrowIfNull(consentResolution);

        return consentResolution.Status switch
        {
            ConsentResolutionStatus.AdminConsentRequired => consentResolution.AdminConsentUri is null
                ? "Administrator consent is required before this hosted tenant can continue."
                : $"Administrator consent is required before this hosted tenant can continue. Ask your administrator to approve access: {consentResolution.AdminConsentUri}",
            ConsentResolutionStatus.Declined => "Consent was declined. Sign in again and complete consent, or contact your administrator.",
            _ => "Hosted consent cannot be validated right now. Retry shortly or contact your administrator.",
        };
    }

    private static IReadOnlyList<string>? ResolveGoalList(string? goal)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            return null;
        }

        return [goal.Trim()];
    }

    private static void ValidateRequest(ImportPlanningRequest request)
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
}
