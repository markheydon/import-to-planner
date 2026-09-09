using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Builds import previews from validated planning requests.
/// </summary>
public sealed class ImportPlanningUseCase(
    IPlannerGateway plannerGateway,
    ICurrentTenantContextAccessor currentTenantContextAccessor,
    ITenantOperationalMetadataStore tenantOperationalMetadataStore,
    ConsentResolutionDefaults consentResolutionDefaults) : IImportPlanningUseCase
{
    private const string DefaultBucketName = "General";
    private const string TaskAlreadyExistsReason = "already exists";
    private const string AssignedToField = "Assigned To";
    private const string NotAMemberReasonCode = "not-a-member";
    private const string NotAnAddressReasonCode = "not-an-address";
    private static readonly IReadOnlyList<string> EmptyAssigneeIds = [];
    private static readonly IReadOnlyList<UnresolvedAssignee> EmptyUnresolvedAssignees = [];

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
            throw new ConsentBlockedException(consentResolution);
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

        var validationFindings = new List<ImportValidationError>();
        IReadOnlyList<PlanMember>? destinationMembers = null;

        if (request.Rows.Any(row => HasAssigneeAddresses(row.AssigneeAddresses)))
        {
            try
            {
                destinationMembers = await plannerGateway.GetPlanMembersAsync(
                    request.ContainerId,
                    request.ContainerType,
                    cancellationToken);
            }
            catch (PlannerOperationException ex)
            {
                validationFindings.Add(new ImportValidationError(
                    0,
                    AssignedToField,
                    ex.Failure.Message));
            }
        }

        var csvSeenTaskNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var taskActions = new List<ImportTaskPlanItem>();

        foreach (var row in request.Rows)
        {
            var resolvedBucket = string.IsNullOrWhiteSpace(row.Bucket)
                ? DefaultBucketName
                : row.Bucket!;

            var assigneeAddresses = NormaliseAssigneeAddresses(row.AssigneeAddresses);
            var assigneeResolution = destinationMembers is null
                ? new AssigneeResolution(EmptyAssigneeIds, EmptyUnresolvedAssignees)
                : ResolveAssignees(assigneeAddresses, destinationMembers);

            if (!csvSeenTaskNames.Add(row.TaskName))
            {
                taskActions.Add(new ImportTaskPlanItem(
                    row.RowNumber,
                    row.TaskName,
                    resolvedBucket,
                    ResolveGoalList(row.Goal),
                    PlannedEntityAction.Skip,
                    "duplicate in CSV",
                    DueDate: row.DueDate,
                    AssigneeAddresses: assigneeAddresses,
                    ResolvedAssigneeIds: EmptyAssigneeIds,
                    UnresolvedAssignees: assigneeResolution.UnresolvedAssignees));

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
                    DueDate: row.DueDate,
                    AssigneeAddresses: assigneeAddresses,
                    ResolvedAssigneeIds: EmptyAssigneeIds,
                    UnresolvedAssignees: assigneeResolution.UnresolvedAssignees));

                continue;
            }

            taskActions.Add(new ImportTaskPlanItem(
                row.RowNumber,
                row.TaskName,
                resolvedBucket,
                ResolveGoalList(row.Goal),
                PlannedEntityAction.Create,
                DueDate: row.DueDate,
                AssigneeAddresses: assigneeAddresses,
                ResolvedAssigneeIds: assigneeResolution.ResolvedAssigneeIds,
                UnresolvedAssignees: assigneeResolution.UnresolvedAssignees));
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
            HasValidationErrors = validationFindings.Count > 0,
            ValidationFindings = validationFindings,
            RequestFingerprint = requestFingerprint,
            PlannerStateFingerprint = plannerStateFingerprint,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = bucketActions,
            TaskActions = taskActions,
        };

        await outputBoundary.PresentAsync(response, cancellationToken);
    }

    private static bool HasAssigneeAddresses(IReadOnlyList<string>? assigneeAddresses)
    {
        return assigneeAddresses is { Count: > 0 };
    }

    private static IReadOnlyList<string> NormaliseAssigneeAddresses(IReadOnlyList<string>? assigneeAddresses)
    {
        if (assigneeAddresses is null || assigneeAddresses.Count == 0)
        {
            return [];
        }

        return assigneeAddresses;
    }

    private static AssigneeResolution ResolveAssignees(
        IReadOnlyList<string> assigneeAddresses,
        IReadOnlyList<PlanMember> destinationMembers)
    {
        if (assigneeAddresses.Count == 0)
        {
            return new AssigneeResolution(EmptyAssigneeIds, EmptyUnresolvedAssignees);
        }

        var resolvedIds = new List<string>();
        var unresolved = new List<UnresolvedAssignee>();
        var resolvedIdSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var address in assigneeAddresses)
        {
            if (!address.Contains('@', StringComparison.Ordinal))
            {
                unresolved.Add(new UnresolvedAssignee(address, NotAnAddressReasonCode));
                continue;
            }

            var matchedMember = destinationMembers.FirstOrDefault(member =>
                MatchesMember(address, member));

            if (matchedMember is null)
            {
                unresolved.Add(new UnresolvedAssignee(address, NotAMemberReasonCode));
                continue;
            }

            if (resolvedIdSet.Add(matchedMember.Id))
            {
                resolvedIds.Add(matchedMember.Id);
            }
        }

        return new AssigneeResolution(resolvedIds, unresolved);
    }

    private static bool MatchesMember(string address, PlanMember member)
    {
        return (!string.IsNullOrWhiteSpace(member.Mail)
                && string.Equals(address, member.Mail, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(member.SignInName)
                && string.Equals(address, member.SignInName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ConsentResolution> ResolveConsentAsync(CancellationToken cancellationToken)
    {
        var tenantContext = currentTenantContextAccessor.GetRequiredContext();
        var metadata = await tenantOperationalMetadataStore.GetAsync(tenantContext.TenantId, cancellationToken).ConfigureAwait(false);

        if (metadata is null)
        {
            return new ConsentResolution(
                ConsentResolutionStatus.UserConsentAvailable,
                consentResolutionDefaults.RequiredScopes,
                consentResolutionDefaults.AdminConsentUri,
                "consent.user-consent-available");
        }

        return metadata.ConsentStatus switch
        {
            ConsentResolutionStatus.Granted => ConsentResolution.Granted(consentResolutionDefaults.RequiredScopes),
            ConsentResolutionStatus.UserConsentAvailable => new ConsentResolution(
                ConsentResolutionStatus.UserConsentAvailable,
                consentResolutionDefaults.RequiredScopes,
                consentResolutionDefaults.AdminConsentUri,
                "consent.user-consent-available"),
            ConsentResolutionStatus.AdminConsentRequired => new ConsentResolution(
                ConsentResolutionStatus.AdminConsentRequired,
                consentResolutionDefaults.RequiredScopes,
                consentResolutionDefaults.AdminConsentUri,
                "consent.admin-consent-required",
                metadata.LastSupportDiagnosticCode),
            ConsentResolutionStatus.Declined => new ConsentResolution(
                ConsentResolutionStatus.Declined,
                consentResolutionDefaults.RequiredScopes,
                consentResolutionDefaults.AdminConsentUri,
                "consent.declined",
                metadata.LastSupportDiagnosticCode),
            _ => new ConsentResolution(
                ConsentResolutionStatus.Unavailable,
                consentResolutionDefaults.RequiredScopes,
                consentResolutionDefaults.AdminConsentUri,
                "consent.unavailable",
                metadata.LastSupportDiagnosticCode),
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

    private sealed record AssigneeResolution(
        IReadOnlyList<string> ResolvedAssigneeIds,
        IReadOnlyList<UnresolvedAssignee> UnresolvedAssignees);
}
