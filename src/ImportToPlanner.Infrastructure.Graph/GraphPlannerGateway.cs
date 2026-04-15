using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Domain;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
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
    private const int MaxGroupPageSize = 999;
    private const int MaxThrottlingRetries = 3;
    private const int MaxConflictRetries = 1;
    private const int DefaultRetryAfterSeconds = 10;
    private const int MaxRetryAfterSeconds = 60;
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

        var me = await ExecuteGraphCallAsync(
            "loading available planner containers",
            innerToken => graphClient.Me.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = ["id", "displayName"];
                },
                innerToken),
            cancellationToken);

        var containers = new List<PlannerContainer>();
        if (!string.IsNullOrWhiteSpace(me?.Id))
        {
            containers.Add(new PlannerContainer(
                me.Id,
                string.IsNullOrWhiteSpace(me.DisplayName) ? "My Personal Plans" : me.DisplayName,
                ContainerType.User));
        }

        var groupsResponse = await ExecuteGraphCallAsync(
            "loading available planner containers",
            innerToken => graphClient.Me.MemberOf.GraphGroup.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = ["id", "displayName"];
                    requestConfiguration.QueryParameters.Filter = "groupTypes/any(c:c eq 'Unified')";
                    requestConfiguration.QueryParameters.Top = MaxGroupPageSize;
                },
                innerToken),
            cancellationToken);

        while (groupsResponse is not null)
        {
            if (groupsResponse.Value is not null)
            {
                containers.AddRange(groupsResponse.Value
                    .Where(group => !string.IsNullOrWhiteSpace(group.Id))
                    .Select(group => new PlannerContainer(
                        group.Id!,
                        string.IsNullOrWhiteSpace(group.DisplayName) ? group.Id! : group.DisplayName,
                        ContainerType.Group)));
            }

            if (string.IsNullOrWhiteSpace(groupsResponse.OdataNextLink))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            groupsResponse = await ExecuteGraphCallAsync(
                "loading available planner containers",
                innerToken => graphClient.Me.MemberOf.GraphGroup
                    .WithUrl(groupsResponse.OdataNextLink)
                    .GetAsync(cancellationToken: innerToken),
                cancellationToken);
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

        Microsoft.Graph.Models.PlannerPlanCollectionResponse? plansResponse;
        try
        {
            plansResponse = await ExecuteGraphCallAsync(
                "finding planner plan by name",
                innerToken => graphClient.Planner.Plans.GetAsync(
                    requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"container/containerId eq '{EscapeODataLiteral(containerId)}'";
                        requestConfiguration.QueryParameters.Select = ["id", "title", "container"];
                    },
                    innerToken),
                cancellationToken);
        }
        catch (PlannerNotFoundException)
        {
            return null;
        }

        while (plansResponse is not null)
        {
            var existing = plansResponse.Value?.FirstOrDefault(plan =>
                string.Equals(plan.Title, planName, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                return MapPlannerPlan(existing, containerId, null);
            }

            if (string.IsNullOrWhiteSpace(plansResponse.OdataNextLink))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            plansResponse = await ExecuteGraphCallAsync(
                "finding planner plan by name",
                innerToken => graphClient.Planner.Plans
                    .WithUrl(plansResponse.OdataNextLink)
                    .GetAsync(cancellationToken: innerToken),
                cancellationToken);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<PlannerPlan> CreatePlanAsync(string containerId, string planName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(containerId, nameof(containerId));
        ValidateRequired(planName, nameof(planName));

        var containerType = await ResolveContainerTypeAsync(containerId, cancellationToken);
        var graphContainerType = containerType == ContainerType.Group ? "group" : "user";

        var created = await ExecuteGraphCallAsync(
            "creating planner plan",
            innerToken => graphClient.Planner.Plans.PostAsync(
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
                cancellationToken: innerToken),
            cancellationToken);

        return MapPlannerPlan(
            created ?? throw new InvalidOperationException("Graph did not return the created plan."),
            containerId,
            containerType);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        var bucketsResponse = await ExecuteGraphCallAsync(
            "loading planner buckets",
            innerToken => graphClient.Planner.Plans[planId].Buckets.GetAsync(cancellationToken: innerToken),
            cancellationToken);
        var buckets = new List<PlannerBucket>();

        while (bucketsResponse is not null)
        {
            if (bucketsResponse.Value is not null)
            {
                buckets.AddRange(bucketsResponse.Value
                    .Where(bucket => !string.IsNullOrWhiteSpace(bucket.Id) && !string.IsNullOrWhiteSpace(bucket.Name))
                    .Select(bucket => new PlannerBucket(bucket.Id!, bucket.Name!, bucket.PlanId ?? planId)));
            }

            if (string.IsNullOrWhiteSpace(bucketsResponse.OdataNextLink))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            bucketsResponse = await ExecuteGraphCallAsync(
                "loading planner buckets",
                innerToken => graphClient.Planner.Plans[planId].Buckets
                    .WithUrl(bucketsResponse.OdataNextLink)
                    .GetAsync(cancellationToken: innerToken),
                cancellationToken);
        }

        return buckets;
    }

    /// <inheritdoc/>
    public async Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ValidateRequired(bucketName, nameof(bucketName));

        var created = await ExecuteGraphCallAsync(
            "creating planner bucket",
            innerToken => graphClient.Planner.Buckets.PostAsync(
                new GraphPlannerBucket
                {
                    Name = bucketName,
                    PlanId = planId,
                },
                cancellationToken: innerToken),
            cancellationToken);

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

        var tasksResponse = await ExecuteGraphCallAsync(
            "loading planner tasks",
            innerToken => graphClient.Planner.Plans[planId].Tasks.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = ["id", "title", "planId"];
                },
                innerToken),
            cancellationToken);

        var tasks = new List<PlannerTaskSnapshot>();
        while (tasksResponse is not null)
        {
            if (tasksResponse.Value is not null)
            {
                tasks.AddRange(tasksResponse.Value
                    .Where(task => !string.IsNullOrWhiteSpace(task.Id) && !string.IsNullOrWhiteSpace(task.Title))
                    .Select(task => new PlannerTaskSnapshot(task.Id!, task.Title!, task.PlanId ?? planId)));
            }

            if (string.IsNullOrWhiteSpace(tasksResponse.OdataNextLink))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            tasksResponse = await ExecuteGraphCallAsync(
                "loading planner tasks",
                innerToken => graphClient.Planner.Plans[planId].Tasks
                    .WithUrl(tasksResponse.OdataNextLink)
                    .GetAsync(cancellationToken: innerToken),
                cancellationToken);
        }

        return tasks;
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
        _ = description;
        _ = goal;

        var created = await ExecuteGraphCallAsync(
            "creating planner task",
            innerToken => graphClient.Planner.Tasks.PostAsync(
                new GraphPlannerTask
                {
                    PlanId = planId,
                    BucketId = bucketId,
                    Title = taskName,
                    Priority = priority,
                },
                cancellationToken: innerToken),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(created?.Id) || string.IsNullOrWhiteSpace(created.Title))
        {
            throw new InvalidOperationException("Graph returned an invalid task response.");
        }

        return new PlannerTaskSnapshot(created.Id, created.Title, created.PlanId ?? planId);
    }

    private static PlannerPlan MapPlannerPlan(
        GraphPlannerPlan graphPlan,
        string? fallbackContainerId,
        ContainerType? fallbackContainerType)
    {
        if (string.IsNullOrWhiteSpace(graphPlan.Id) || string.IsNullOrWhiteSpace(graphPlan.Title))
        {
            throw new InvalidOperationException("Graph returned an invalid plan response.");
        }

        var containerId = graphPlan.Container?.ContainerId ?? fallbackContainerId;
        var containerType = graphPlan.Container is null
            ? fallbackContainerType
            : ResolveContainerTypeFromGraphValue(graphPlan.Container.Type?.ToString(), graphPlan.Container.AdditionalData);
        containerType ??= fallbackContainerType;

        return new Domain.PlannerPlan(graphPlan.Id, graphPlan.Title, containerId, containerType);
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

    private static ContainerType? ResolveContainerTypeFromGraphValue(string? type, IDictionary<string, object>? additionalData)
    {
        var resolvedType = type;
        if (string.IsNullOrWhiteSpace(resolvedType) &&
            additionalData is not null &&
            additionalData.TryGetValue("type", out var value))
        {
            resolvedType = value?.ToString();
        }

        if (string.Equals(resolvedType, "user", StringComparison.OrdinalIgnoreCase))
        {
            return ContainerType.User;
        }

        if (string.Equals(resolvedType, "group", StringComparison.OrdinalIgnoreCase))
        {
            return ContainerType.Group;
        }

        return null;
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

    private static bool TryGetStatusCode(Exception exception, out int statusCode)
    {
        if (exception is ApiException apiException)
        {
            statusCode = apiException.ResponseStatusCode;
            return statusCode > 0;
        }

        statusCode = 0;
        return false;
    }

    private static int ResolveRetryAfterSeconds(ApiException apiException)
    {
        if (apiException.ResponseHeaders is not null)
        {
            foreach (var header in apiException.ResponseHeaders)
            {
                if (!string.Equals(header.Key, "Retry-After", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var retryAfterValue = header.Value?.FirstOrDefault();
                if (int.TryParse(retryAfterValue, out var parsedSeconds))
                {
                    return Math.Clamp(parsedSeconds, 0, MaxRetryAfterSeconds);
                }
            }
        }

        return DefaultRetryAfterSeconds;
    }

    private static void ThrowMappedException(Exception exception, int statusCode, string operationName)
    {
        switch (statusCode)
        {
            case 401:
                throw new PlannerAuthenticationException(
                    "Authentication failed. Please sign in again.",
                    exception);

            case 403:
                throw new PlannerPermissionException(
                    "Insufficient permissions. Ensure the app has Tasks.ReadWrite and Group.Read.All consent.",
                    exception);

            case 404:
                throw new PlannerNotFoundException(
                    $"A Planner resource was not found while {operationName}.",
                    exception);

            default:
                throw new InvalidOperationException(
                    $"Microsoft Graph returned HTTP {statusCode} while {operationName}.",
                    exception);
        }
    }

    private static async Task<T> ExecuteGraphCallAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> graphCall,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(graphCall);

        var throttlingRetries = 0;
        var conflictRetries = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await graphCall(cancellationToken);
            }
            catch (Exception ex) when (TryGetStatusCode(ex, out var statusCode))
            {
                if (statusCode == 429)
                {
                    if (throttlingRetries >= MaxThrottlingRetries)
                    {
                        throw new PlannerThrottlingException(
                            $"Microsoft Graph throttled the request while {operationName} after {MaxThrottlingRetries} retries.",
                            ex);
                    }

                    throttlingRetries++;
                    var retryAfter = ResolveRetryAfterSeconds((ApiException)ex);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
                    continue;
                }

                if (statusCode is 409 or 412)
                {
                    if (conflictRetries >= MaxConflictRetries)
                    {
                        throw new PlannerConflictException(
                            $"Microsoft Graph reported a conflict while {operationName}.",
                            ex);
                    }

                    conflictRetries++;
                    continue;
                }

                ThrowMappedException(ex, statusCode, operationName);
            }
        }
    }
}
