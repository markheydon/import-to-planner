using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Domain;
using Microsoft.Graph;
using GraphPlannerBucket = Microsoft.Graph.Models.PlannerBucket;
using GraphPlannerPlan = Microsoft.Graph.Models.PlannerPlan;
using GraphPlannerPlanContainer = Microsoft.Graph.Models.PlannerPlanContainer;
using GraphPlannerTask = Microsoft.Graph.Models.PlannerTask;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Provides Planner operations backed by Microsoft Graph beta API.
/// </summary>
public sealed class GraphPlannerGateway : IPlannerGateway
{
    private const string BetaBaseUrl = "https://graph.microsoft.com/beta";
    private readonly GraphServiceClient graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphPlannerGateway"/> class.
    /// </summary>
    /// <param name="graphServiceClient">The delegated Graph client.</param>
    public GraphPlannerGateway(GraphServiceClient graphServiceClient)
    {
        ArgumentNullException.ThrowIfNull(graphServiceClient);
        graphClient = new GraphServiceClient(graphServiceClient.RequestAdapter, BetaBaseUrl);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var me = await graphClient.Me.GetAsync(
            requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = ["id", "displayName"];
            },
            cancellationToken);

        var containers = new List<PlannerContainer>();
        if (!string.IsNullOrWhiteSpace(me?.Id))
        {
            containers.Add(new PlannerContainer(
                me.Id,
                string.IsNullOrWhiteSpace(me.DisplayName) ? "My Personal Plans" : me.DisplayName,
                ContainerType.User));
        }

        var groupsResponse = await graphClient.Me.MemberOf.GraphGroup.GetAsync(
            requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = ["id", "displayName"];
            },
            cancellationToken);

        if (groupsResponse?.Value is not null)
        {
            containers.AddRange(groupsResponse.Value
                .Where(group => !string.IsNullOrWhiteSpace(group.Id))
                .Select(group => new PlannerContainer(
                    group.Id!,
                    string.IsNullOrWhiteSpace(group.DisplayName) ? group.Id! : group.DisplayName,
                    ContainerType.Group)));
        }

        return containers
            .DistinctBy(container => container.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(container => container.Type)
            .ThenBy(container => container.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<PlannerPlan?> FindPlanByNameAsync(string containerId, string planName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(containerId, nameof(containerId));
        ValidateRequired(planName, nameof(planName));

        var plansResponse = await graphClient.Planner.Plans.GetAsync(
            requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"container/containerId eq '{EscapeODataLiteral(containerId)}'";
                requestConfiguration.QueryParameters.Select = ["id", "title", "container"];
            },
            cancellationToken);

        var existing = plansResponse?.Value?.FirstOrDefault(plan =>
            string.Equals(plan.Title, planName, StringComparison.OrdinalIgnoreCase));

        return existing is null ? null : MapPlannerPlan(existing);
    }

    /// <inheritdoc/>
    public async Task<PlannerPlan> CreatePlanAsync(string containerId, string planName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(containerId, nameof(containerId));
        ValidateRequired(planName, nameof(planName));

        var containerType = await ResolveContainerTypeAsync(containerId, cancellationToken);
        var graphContainerType = containerType == ContainerType.Group ? "group" : "user";

        var created = await graphClient.Planner.Plans.PostAsync(
            new GraphPlannerPlan
            {
                Title = planName,
                Container = new GraphPlannerPlanContainer
                {
                    ContainerId = containerId,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["type"] = graphContainerType,
                    },
                },
            },
            cancellationToken: cancellationToken);

        return MapPlannerPlan(created ?? throw new InvalidOperationException("Graph did not return the created plan."));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        var bucketsResponse = await graphClient.Planner.Plans[planId].Buckets.GetAsync(cancellationToken: cancellationToken);
        if (bucketsResponse?.Value is null)
        {
            return [];
        }

        return bucketsResponse.Value
            .Where(bucket => !string.IsNullOrWhiteSpace(bucket.Id) && !string.IsNullOrWhiteSpace(bucket.Name))
            .Select(bucket => new PlannerBucket(bucket.Id!, bucket.Name!, bucket.PlanId ?? planId))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ValidateRequired(bucketName, nameof(bucketName));

        var created = await graphClient.Planner.Buckets.PostAsync(
            new GraphPlannerBucket
            {
                Name = bucketName,
                PlanId = planId,
            },
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(created?.Id) || string.IsNullOrWhiteSpace(created.Name))
        {
            throw new InvalidOperationException("Graph returned an invalid bucket response.");
        }

        return new Domain.PlannerBucket(created.Id, created.Name, created.PlanId ?? planId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        var tasksResponse = await graphClient.Planner.Plans[planId].Tasks.GetAsync(
            requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = ["id", "title", "planId"];
            },
            cancellationToken);

        if (tasksResponse?.Value is null)
        {
            return [];
        }

        return tasksResponse.Value
            .Where(task => !string.IsNullOrWhiteSpace(task.Id) && !string.IsNullOrWhiteSpace(task.Title))
            .Select(task => new PlannerTaskSnapshot(task.Id!, task.Title!, task.PlanId ?? planId))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<PlannerTaskSnapshot> CreateTaskAsync(
        string planId,
        string bucketId,
        string taskName,
        string? description,
        int? priority,
        string? goal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ValidateRequired(bucketId, nameof(bucketId));
        ValidateRequired(taskName, nameof(taskName));

        var mappedPriority = MapPriority(priority);
        var created = await graphClient.Planner.Tasks.PostAsync(
            new GraphPlannerTask
            {
                PlanId = planId,
                BucketId = bucketId,
                Title = taskName,
                Priority = mappedPriority,
            },
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(created?.Id) || string.IsNullOrWhiteSpace(created.Title))
        {
            throw new InvalidOperationException("Graph returned an invalid task response.");
        }

        return new PlannerTaskSnapshot(created.Id, created.Title, created.PlanId ?? planId);
    }

    private static PlannerPlan MapPlannerPlan(GraphPlannerPlan graphPlan)
    {
        if (string.IsNullOrWhiteSpace(graphPlan.Id) || string.IsNullOrWhiteSpace(graphPlan.Title))
        {
            throw new InvalidOperationException("Graph returned an invalid plan response.");
        }

        var containerType = graphPlan.Container is null
            ? (ContainerType?)null
            : ResolveContainerTypeFromGraphValue(graphPlan.Container.Type?.ToString(), graphPlan.Container.AdditionalData);

        return new Domain.PlannerPlan(graphPlan.Id, graphPlan.Title, graphPlan.Container?.ContainerId, containerType);
    }

    private async Task<ContainerType> ResolveContainerTypeAsync(string containerId, CancellationToken cancellationToken)
    {
        var containers = await GetAvailableContainersAsync(cancellationToken);
        var container = containers.FirstOrDefault(existing => string.Equals(existing.Id, containerId, StringComparison.OrdinalIgnoreCase));
        if (container is null)
        {
            throw new InvalidOperationException($"Container '{containerId}' is not available to the current user.");
        }

        return container.Type;
    }

    private static ContainerType ResolveContainerTypeFromGraphValue(string? type, IDictionary<string, object>? additionalData)
    {
        var resolvedType = type;
        if (string.IsNullOrWhiteSpace(resolvedType) &&
            additionalData is not null &&
            additionalData.TryGetValue("type", out var value))
        {
            resolvedType = value?.ToString();
        }

        return string.Equals(resolvedType, "user", StringComparison.OrdinalIgnoreCase)
            ? ContainerType.User
            : ContainerType.Group;
    }

    private static int? MapPriority(int? priority)
    {
        if (priority is null)
        {
            return null;
        }

        return priority.Value switch
        {
            1 => 1,
            3 => 3,
            5 => 5,
            9 => 9,
            _ => priority.Value,
        };
    }

    private static string EscapeODataLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }
    }
}
