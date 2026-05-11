using Bunit;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class HomePageTestContext : BunitContext
{
    public HomePageTestContext()
    {
        Services.AddFluentUIComponents();
        Services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlannerGateway:UseGraph"] = "false",
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<AuthenticationStateProvider, FakeAuthenticationStateProvider>();
        Services.AddScoped<ICsvImportParser, StubCsvImportParser>();
        Services.AddScoped<IImportPlannerOrchestrator>(_ => Orchestrator);
        Services.AddScoped<IPlannerGateway>(_ => Gateway);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public StubImportPlannerOrchestrator Orchestrator { get; } = new();

    public StubPlannerGateway Gateway { get; } = new();
}

internal sealed class FakeAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(anonymous));
    }
}

internal sealed class StubCsvImportParser : ICsvImportParser
{
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)
    {
        return Task.FromResult(new CsvParseResult(
            [new CsvTaskRow(2, "Stub Task", null, null, null, null)],
            []));
    }
}

internal sealed class StubPlannerGateway : IPlannerGateway
{
    public IReadOnlyList<PlannerContainer> Containers { get; set; } =
    [
        new PlannerContainer("container-1", "Test Container", ContainerType.Group),
    ];

    public IReadOnlyList<PlannerPlan> Plans { get; set; } =
    [
        new PlannerPlan("plan-1", "Test Plan", "container-1", ContainerType.Group),
    ];

    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
        => Task.FromResult(Containers);

    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult(Plans.FirstOrDefault(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
        => Task.FromResult(Plans);

    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerBucket>>([]);

    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        => Task.FromResult(new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId));

    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);

    public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
        => Task.FromResult(new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId));
}

internal sealed class StubImportPlannerOrchestrator : IImportPlannerOrchestrator
{
    public ImportPlanPreview? PreviewToReturn { get; set; }

    public ImportExecutionResult? ExecutionResultToReturn { get; set; }

    public Task<ImportPlanPreview> BuildPreviewAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        var preview = PreviewToReturn ?? new ImportPlanPreview
        {
            ContainerId = request.ContainerId,
            PlanId = request.PlanId,
            PlanName = request.PlanName,
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = false,
            RequestFingerprint = "stub-request",
            PlannerStateFingerprint = "stub-state",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase),
            TaskActions = [],
        };

        return Task.FromResult(preview);
    }

    public Task<ImportExecutionResult> ExecuteAsync(ImportRequest request, ImportPlanPreview preview, CancellationToken cancellationToken)
    {
        var result = ExecutionResultToReturn ?? new ImportExecutionResult
        {
            PlanId = preview.PlanId,
            Created = ["Task: Stub Task"],
            ReusedOrSkipped = [],
            Errors = [],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(1, 0, 0, 0, false, false),
        };

        return Task.FromResult(result);
    }
}
