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
