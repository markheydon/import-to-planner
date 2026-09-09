using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Domain;
using ImportToPlanner.Tests.TestDoubles;

namespace ImportToPlanner.Tests;

public sealed class ImportPlanningUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithDuplicateRows_SkipsSecondDuplicate()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();

        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Task A", "One", 3, "Ops", "Goal 1"),
                new CsvTaskRow(3, "Task A", "Two", 3, "Ops", "Goal 1"),
            ]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        var preview = output.Response!;
        Assert.Equal(2, preview.TaskActions.Count);
        Assert.Equal(PlannedEntityAction.Create, preview.TaskActions[0].Action);
        Assert.Equal(PlannedEntityAction.Skip, preview.TaskActions[1].Action);
    }

    [Fact]
    public async Task HandleAsync_WithMissingBucket_UsesGeneralBucket()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        var preview = output.Response!;
        Assert.Contains(preview.BucketActions, bucket =>
            string.Equals(bucket.Key, "General", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.TaskActions, task =>
            string.Equals(task.Bucket, "General", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsync_WhenConsentMetadataMissing_BuildsPreviewUsingSinglePathDefaults()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        Assert.Equal("plan-a", output.Response!.PlanId);
    }

    [Fact]
    public async Task HandleAsync_WhenConsentRequiresAdministrator_ThrowsConsentBlockedException()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");

        var metadataStore = new TenantOperationalMetadataStoreStub();
        await metadataStore.UpsertAsync(
            new TenantOperationalMetadata(
                "tenant-a",
                ConsentResolutionStatus.AdminConsentRequired,
                null,
                DateTimeOffset.UtcNow,
                "AdminConsentRequired",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var useCase = CreateUseCase(gateway, metadataStore);
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        var ex = await Assert.ThrowsAsync<ConsentBlockedException>(() =>
            useCase.HandleAsync(request, new CapturePlanningOutputBoundary(), CancellationToken.None));

        Assert.Equal(ConsentResolutionStatus.AdminConsentRequired, ex.Resolution.Status);
    }

    [Fact]
    public async Task HandleAsync_WithDueDate_PopulatesPreviewDueDate()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var dueDate = new DateOnly(2026, 5, 31);
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, dueDate)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(dueDate, task.DueDate);
        Assert.Equal(PlannedEntityAction.Create, task.Action);
    }

    [Fact]
    public async Task HandleAsync_WithExistingTaskAndDueDate_SkipsWhileRetainingDueDateForDisplay()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var bucket = await gateway.CreateBucketAsync("plan-a", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-a", bucket.Id, "Existing Task", null, null, null, null, [], CancellationToken.None);
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var dueDate = new DateOnly(2026, 6, 15);
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Existing Task", null, null, "Ops", null, dueDate)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(PlannedEntityAction.Skip, task.Action);
        Assert.Equal("already exists", task.Reason);
        Assert.Equal(dueDate, task.DueDate);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCsvRowAndDueDate_SkipsWhileRetainingDueDateForDisplay()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var dueDate = new DateOnly(2026, 5, 31);
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Task A", null, null, "Ops", null, dueDate),
                new CsvTaskRow(3, "Task A", null, null, "Ops", null, dueDate),
            ]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.Equal(2, output.Response!.TaskActions.Count);
        var duplicate = Assert.Single(output.Response.TaskActions, task => task.Reason == "duplicate in CSV");
        Assert.Equal(dueDate, duplicate.DueDate);
    }

    [Fact]
    public async Task HandleAsync_WhenDueDateChanges_ProducesDifferentRequestFingerprint()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var firstOutput = new CapturePlanningOutputBoundary();
        var secondOutput = new CapturePlanningOutputBoundary();
        var firstRequest = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, new DateOnly(2026, 5, 31))]);
        var secondRequest = firstRequest with
        {
            Rows = [new CsvTaskRow(2, "Task A", null, null, "Ops", null, new DateOnly(2026, 6, 1))],
        };

        await useCase.HandleAsync(firstRequest, firstOutput, CancellationToken.None);
        await useCase.HandleAsync(secondRequest, secondOutput, CancellationToken.None);

        Assert.NotEqual(firstOutput.Response!.RequestFingerprint, secondOutput.Response!.RequestFingerprint);
    }

    [Fact]
    public async Task HandleAsync_WithoutAssigneeAddresses_DoesNotCallGetPlanMembers()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.Equal(0, gateway.GetPlanMembersCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenMemberLookupFails_SetsAssignedToValidationError()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.GetPlanMembersException = new PlannerOperationException(new PlannerOperationFailure(
            PlannerFailureCategory.Authorisation,
            PlannerFailureTarget.Container,
            "group-a",
            "Destination members could not be loaded.",
            false,
            "Authorisation"));
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["a@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.True(output.Response!.HasValidationErrors);
        Assert.Contains(output.Response.ValidationFindings, error =>
            error.RowNumber == 0 && error.Field == "Assigned To");
    }

    [Fact]
    public async Task HandleAsync_WithMailMatch_ResolvesMemberIdOnCreateRow()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["a@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(PlannedEntityAction.Create, task.Action);
        Assert.Equal(["user-1"], task.ResolvedAssigneeIds);
    }

    [Fact]
    public async Task HandleAsync_WithUpnMatch_ResolvesMemberIdOnCreateRow()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["guest_external.com#EXT#@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(["guest-1"], task.ResolvedAssigneeIds);
    }

    [Fact]
    public async Task HandleAsync_WithGuestAlreadyInDestination_ResolvesGuestMemberId()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["guest@external.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(["guest-1"], task.ResolvedAssigneeIds);
    }

    [Fact]
    public async Task HandleAsync_WithAliasOnlyValue_MarksUnresolvedWithNotAMember()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["alias@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Empty(task.ResolvedAssigneeIds!);
        var unresolved = Assert.Single(task.UnresolvedAssignees!);
        Assert.Equal("alias@contoso.com", unresolved.Address);
        Assert.Equal("not-a-member", unresolved.ReasonCode);
    }

    [Fact]
    public async Task HandleAsync_WithMixedAssigneeCell_ShowsResolvedAndUnresolvedWithoutBlockingPreview()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["a@contoso.com", "unknown@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.False(output.Response.HasValidationErrors);
        Assert.Equal(PlannedEntityAction.Create, task.Action);
        Assert.Equal(["user-1"], task.ResolvedAssigneeIds);
        var unresolved = Assert.Single(task.UnresolvedAssignees!);
        Assert.Equal("unknown@contoso.com", unresolved.Address);
        Assert.Equal("not-a-member", unresolved.ReasonCode);
    }

    [Fact]
    public async Task HandleAsync_WithExistingTaskAndAssignees_SkipsWhileRetainingAssigneeAddresses()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.SeedDefaultMembers();
        var bucket = await gateway.CreateBucketAsync("plan-a", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-a", bucket.Id, "Existing Task", null, null, null, null, [], CancellationToken.None);
        var useCase = CreateUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Existing Task", null, null, "Ops", null, null, ["a@contoso.com"])]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        var task = Assert.Single(output.Response!.TaskActions);
        Assert.Equal(PlannedEntityAction.Skip, task.Action);
        Assert.Equal("already exists", task.Reason);
        Assert.Equal(["a@contoso.com"], task.AssigneeAddresses);
        Assert.Empty(task.ResolvedAssigneeIds!);
    }

    [Fact]
    public async Task HandleAsync_WhenAssigneeAddressesChange_ProducesDifferentRequestFingerprint()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = CreateUseCase(gateway);
        var firstOutput = new CapturePlanningOutputBoundary();
        var secondOutput = new CapturePlanningOutputBoundary();
        var firstRequest = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["a@contoso.com"])]);
        var secondRequest = firstRequest with
        {
            Rows = [new CsvTaskRow(2, "Task A", null, null, "Ops", null, null, ["b@contoso.com"])],
        };

        await useCase.HandleAsync(firstRequest, firstOutput, CancellationToken.None);
        await useCase.HandleAsync(secondRequest, secondOutput, CancellationToken.None);

        Assert.NotEqual(firstOutput.Response!.RequestFingerprint, secondOutput.Response!.RequestFingerprint);
    }

    private static ImportPlanningUseCase CreateUseCase(
        IPlannerGateway gateway,
        ITenantOperationalMetadataStore? metadataStore = null)
    {
        return new ImportPlanningUseCase(
            gateway,
            new CurrentTenantContextAccessorStub(),
            metadataStore ?? new TenantOperationalMetadataStoreStub(),
            new ConsentResolutionDefaults(
                ["Tasks.ReadWrite"],
                new Uri("https://example.test/admin-consent")));
    }

    private sealed class CapturePlanningOutputBoundary : IImportPlanningOutputBoundary
    {
        public ImportPlanPreview? Response { get; private set; }

        public Task PresentAsync(ImportPlanPreview response, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Response = response;
            return Task.CompletedTask;
        }
    }

}
