using Bunit;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Workflows;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class HomePageTestContext : BunitContext
{
    public HomePageTestContext(bool useGraphGateway = false)
    {
        Services.AddMudServices(configuration =>
        {
            configuration.PopoverOptions.CheckForPopoverProvider = false;
        });
        AddAuthorization().SetNotAuthorized();
        Services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlannerGateway:UseGraph"] = useGraphGateway ? "true" : "false",
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<AuthenticationStateProvider, FakeAuthenticationStateProvider>();
        Services.AddScoped<ICsvImportParser, StubCsvImportParser>();
        Services.AddScoped<IPlannerGateway>(_ => Gateway);
        Services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        Services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();
        Services.AddScoped<ImportPlanningPresenter>();
        Services.AddScoped<ImportExecutionPresenter>();
        Services.AddScoped<WorkflowCoordinationState>();
        Services.AddScoped<ImportWorkflowCoordinator>();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

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
    public Exception? GetAvailableContainersException { get; set; }

    public Exception? GetPlansException { get; set; }

    public Exception? CreateTaskException { get; set; }

    public IReadOnlyList<PlannerContainer> Containers { get; set; } =
    [
        new PlannerContainer("container-1", "Test Container", ContainerType.Group),
    ];

    public IReadOnlyList<PlannerPlan> Plans { get; set; } =
    [
        new PlannerPlan("plan-1", "Test Plan", "container-1", ContainerType.Group),
    ];

    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
    {
        if (GetAvailableContainersException is not null)
        {
            return Task.FromException<IReadOnlyList<PlannerContainer>>(GetAvailableContainersException);
        }

        return Task.FromResult(Containers);
    }

    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult(Plans.FirstOrDefault(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
    {
        if (GetPlansException is not null)
        {
            return Task.FromException<IReadOnlyList<PlannerPlan>>(GetPlansException);
        }

        return Task.FromResult(Plans);
    }

    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerBucket>>([]);

    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        => Task.FromResult(new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId));

    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);

    public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
    {
        if (CreateTaskException is not null)
        {
            return Task.FromException<PlannerTaskSnapshot>(CreateTaskException);
        }

        return Task.FromResult(new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId));
    }

    public static PlannerOperationException AuthenticationFailure()
    {
        return new PlannerOperationException(new PlannerOperationFailure(
            PlannerFailureCategory.Authentication,
            PlannerFailureTarget.Workflow,
            null,
            "Authentication failed.",
            false,
            "Authentication"));
    }
}
