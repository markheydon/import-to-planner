using System.Globalization;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Domain;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using GraphPlannerBucket = Microsoft.Graph.Models.PlannerBucket;
using GraphPlannerPlan = Microsoft.Graph.Models.PlannerPlan;
using GraphPlannerTask = Microsoft.Graph.Models.PlannerTask;

namespace ImportToPlanner.Tests;

public sealed class GraphPlannerGatewayTests
{
    [Fact]
    public async Task GetAvailableContainersAsync_WithUserAndGroups_ReturnsDistinctContainers()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<User>(
            "me",
            request => request.URI?.AbsolutePath.EndsWith("/me", StringComparison.OrdinalIgnoreCase) == true,
            new User { Id = "user-1", DisplayName = "Mark" });
        adapter.QueueSendAsyncResponse<GroupCollectionResponse>(
            "memberOf",
            request => request.URI?.AbsolutePath.Contains("/memberOf/", StringComparison.OrdinalIgnoreCase) == true,
            new GroupCollectionResponse
            {
                Value =
                [
                    new Group { Id = "group-1", DisplayName = "Alpha" },
                    new Group { Id = "group-1", DisplayName = "Alpha duplicate" },
                ],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetAvailableContainersAsync(CancellationToken.None);

        // Assert
        Assert.Contains(result, container =>
            container.Type == ContainerType.User &&
            container.Id == "user-1");
        Assert.Single(result, container =>
            container.Type == ContainerType.Group &&
            container.Id == "group-1");
    }

    [Fact]
    public async Task GetPlansAsync_WithNoPlans_ReturnsEmptyList()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerPlanCollectionResponse>(
            "plans",
            request => request.URI?.AbsolutePath.EndsWith("/groups/group-1/planner/plans", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerPlanCollectionResponse
            {
                Value = [],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetPlansAsync("group-1", ContainerType.Group, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPlanByIdAsync_WithRosterContainer_ReturnsRawContainerDetails()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerPlan>(
            "planById",
            request => request.URI?.AbsolutePath.EndsWith("/planner/plans/plan-1", StringComparison.OrdinalIgnoreCase) == true,
            new GraphPlannerPlan
            {
                Id = "plan-1",
                Title = "Personal Board",
                Container = new PlannerPlanContainer
                {
                    ContainerId = "roster-1",
                    Type = PlannerContainerType.UnknownFutureValue,
                    Url = "https://graph.microsoft.com/beta/planner/rosters/roster-1",
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["type"] = "roster",
                    },
                },
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetPlanByIdAsync("plan-1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("plan-1", result.Id);
        Assert.Equal("Personal Board", result.Title);
        Assert.Equal("roster-1", result.ContainerId);
        Assert.Equal(ContainerType.Roster, result.ContainerType);
        Assert.Equal("roster", result.RawContainerType);
        Assert.Equal("https://graph.microsoft.com/beta/planner/rosters/roster-1", result.ContainerUrl);
    }

    [Fact]
    public async Task GetPlanByIdAsync_WithUserContainerUrl_InfersUserContainerType()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerPlan>(
            "planByIdUserContainer",
            request => request.URI?.AbsolutePath.EndsWith("/planner/plans/plan-2", StringComparison.OrdinalIgnoreCase) == true,
            new GraphPlannerPlan
            {
                Id = "plan-2",
                Title = "Personal Plan",
                Container = new PlannerPlanContainer
                {
                    ContainerId = "b55eed95-2e13-4f07-84ef-65d8ddb048dc",
                    Url = "https://graph.microsoft.com/beta/users/b55eed95-2e13-4f07-84ef-65d8ddb048dc",
                },
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetPlanByIdAsync("plan-2", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ContainerType.User, result.ContainerType);
        Assert.Equal("user", result.RawContainerType);
        Assert.Equal("https://graph.microsoft.com/beta/users/b55eed95-2e13-4f07-84ef-65d8ddb048dc", result.ContainerUrl);
    }

    [Fact]
    public async Task GetPlansAsync_WithMatchingPlans_ReturnsMappedPlans()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerPlanCollectionResponse>(
            "plans",
            request => request.URI?.AbsolutePath.EndsWith("/groups/group-1/planner/plans", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerPlanCollectionResponse
            {
                Value =
                [
                    new GraphPlannerPlan
                    {
                        Id = "plan-1",
                        Title = "Team Plan",
                        Container = new PlannerPlanContainer
                        {
                            ContainerId = "group-1",
                            Type = PlannerContainerType.Group,
                        },
                    },
                ],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetPlansAsync("group-1", ContainerType.Group, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("plan-1", result[0].Id);
        Assert.Equal("Team Plan", result[0].Title);
        Assert.Equal("group-1", result[0].ContainerId);
        Assert.Equal(ContainerType.Group, result[0].ContainerType);
    }

    [Fact]
    public async Task GetPlansAsync_WithUserContainer_FiltersUnknownContainerTypes()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerPlanCollectionResponse>(
            "userPlans",
            request => request.URI?.AbsolutePath.EndsWith("/users/user-1/planner/plans", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerPlanCollectionResponse
            {
                Value =
                [
                    new GraphPlannerPlan
                    {
                        Id = "personal-plan",
                        Title = "Personal Plan",
                        Container = new PlannerPlanContainer
                        {
                            ContainerId = "user-1",
                            Type = PlannerContainerType.UnknownFutureValue,
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["type"] = "user",
                            },
                        },
                    },
                    new GraphPlannerPlan
                    {
                        Id = "unknown-type-plan",
                        Title = "Unknown Type Plan",
                        Container = new PlannerPlanContainer
                        {
                            ContainerId = "workspace-1",
                            Type = PlannerContainerType.UnknownFutureValue,
                        },
                    },
                ],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetPlansAsync("user-1", ContainerType.User, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("personal-plan", result[0].Id);
        Assert.Equal(ContainerType.User, result[0].ContainerType);
    }

    [Fact]
    public async Task GetBucketsAsync_WithBucketsResponse_ReturnsMappedBuckets()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerBucketCollectionResponse>(
            "getBuckets",
            request => request.URI?.AbsolutePath.EndsWith("/planner/plans/plan-1/buckets", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerBucketCollectionResponse
            {
                Value =
                [
                    new GraphPlannerBucket { Id = "bucket-1", Name = "General", PlanId = "plan-1" },
                    new GraphPlannerBucket { Id = "bucket-2", Name = "Ops", PlanId = "plan-1" },
                ],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetBucketsAsync("plan-1", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, bucket => bucket.Id == "bucket-1" && bucket.Name == "General");
        Assert.Contains(result, bucket => bucket.Id == "bucket-2" && bucket.Name == "Ops");
    }

    [Fact]
    public async Task CreateBucketAsync_WithValidInput_ReturnsCreatedBucket()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerBucket>(
            "createBucket",
            request => request.URI?.AbsolutePath.EndsWith("/planner/buckets", StringComparison.OrdinalIgnoreCase) == true,
            new GraphPlannerBucket { Id = "bucket-1", Name = "Ops", PlanId = "plan-1" });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.CreateBucketAsync("plan-1", "Ops", CancellationToken.None);

        // Assert
        Assert.Equal("bucket-1", result.Id);
        Assert.Equal("Ops", result.Name);
        Assert.Equal("plan-1", result.PlanId);
    }

    [Fact]
    public async Task GetTasksAsync_WithTasksResponse_ReturnsMappedTasks()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerTaskCollectionResponse>(
            "getTasks",
            request => request.URI?.AbsolutePath.EndsWith("/planner/plans/plan-1/tasks", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerTaskCollectionResponse
            {
                Value =
                [
                    new GraphPlannerTask { Id = "task-1", Title = "Task A", PlanId = "plan-1" },
                    new GraphPlannerTask { Id = "task-2", Title = "Task B", PlanId = "plan-1" },
                ],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetTasksAsync("plan-1", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, task => task.Id == "task-1" && task.Title == "Task A");
        Assert.Contains(result, task => task.Id == "task-2" && task.Title == "Task B");
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidInput_ReturnsCreatedTask()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerTask>(
            "createTaskSuccess",
            request => request.URI?.AbsolutePath.EndsWith("/planner/tasks", StringComparison.OrdinalIgnoreCase) == true,
            new GraphPlannerTask { Id = "task-1", Title = "Task A", PlanId = "plan-1" });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.CreateTaskAsync("plan-1", "bucket-1", "Task A", "Desc", 3, null, CancellationToken.None);

        // Assert
        Assert.Equal("task-1", result.Id);
        Assert.Equal("Task A", result.Title);
        Assert.Equal("plan-1", result.PlanId);
    }

    [Fact]
    public async Task GetAvailableContainersAsync_WhenGraphReturns401_ThrowsPlannerAuthenticationException()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<User>(
            "me",
            request => request.URI?.AbsolutePath.EndsWith("/me", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(401));
        adapter.QueueSendAsyncResponse<GroupCollectionResponse>(
            "memberOf",
            request => request.URI?.AbsolutePath.Contains("/memberOf/", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(401));

        var gateway = CreateGateway(adapter);

        // Act + Assert
        await Assert.ThrowsAsync<PlannerAuthenticationException>(() =>
            gateway.GetAvailableContainersAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CreateTaskAsync_WhenGraphReturns403_ThrowsPlannerPermissionException()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerTask>(
            "createTask",
            request => request.URI?.AbsolutePath.EndsWith("/planner/tasks", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(403));

        var gateway = CreateGateway(adapter);

        // Act + Assert
        await Assert.ThrowsAsync<PlannerPermissionException>(() =>
            gateway.CreateTaskAsync("plan-1", "bucket-1", "Task A", null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAvailableContainersAsync_WhenThrottledThenSuccess_RetriesAndReturns()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<User>(
            "me",
            request => request.URI?.AbsolutePath.EndsWith("/me", StringComparison.OrdinalIgnoreCase) == true,
            new User { Id = "user-1", DisplayName = "Mark" });
        adapter.QueueSendAsyncResponse<GroupCollectionResponse>(
            "memberOf",
            request => request.URI?.AbsolutePath.Contains("/memberOf/", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(429, retryAfterSeconds: 0),
            new GroupCollectionResponse { Value = [new Group { Id = "group-1", DisplayName = "Alpha" }] });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.GetAvailableContainersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, adapter.GetCallCount("memberOf"));
    }

    [Fact]
    public async Task GetAvailableContainersAsync_WhenThrottledBeyondRetryLimit_ThrowsPlannerThrottlingException()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<User>(
            "me",
            request => request.URI?.AbsolutePath.EndsWith("/me", StringComparison.OrdinalIgnoreCase) == true,
            new User { Id = "user-1", DisplayName = "Mark" });
        adapter.QueueSendAsyncResponse<GroupCollectionResponse>(
            "memberOf",
            request => request.URI?.AbsolutePath.Contains("/memberOf/", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(429, retryAfterSeconds: 0),
            CreateApiException(429, retryAfterSeconds: 0),
            CreateApiException(429, retryAfterSeconds: 0),
            CreateApiException(429, retryAfterSeconds: 0));

        var gateway = CreateGateway(adapter);

        // Act + Assert
        await Assert.ThrowsAsync<PlannerThrottlingException>(() =>
            gateway.GetAvailableContainersAsync(CancellationToken.None));
        Assert.Equal(4, adapter.GetCallCount("memberOf"));
    }

    [Fact]
    public async Task CreateTaskAsync_WhenGraphReturnsConflict_ThrowsPlannerConflictExceptionWithoutBlindRetry()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<GraphPlannerTask>(
            "createTask",
            request => request.URI?.AbsolutePath.EndsWith("/planner/tasks", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(412));

        var gateway = CreateGateway(adapter);

        // Act + Assert
        await Assert.ThrowsAsync<PlannerConflictException>(() =>
            gateway.CreateTaskAsync("plan-1", "bucket-1", "Task A", null, null, null, CancellationToken.None));
        Assert.Equal(1, adapter.GetCallCount("createTask"));
    }

    private static GraphPlannerGateway CreateGateway(IRequestAdapter requestAdapter)
    {
        var client = new GraphServiceClient(requestAdapter);
        return new GraphPlannerGateway(client);
    }

    private static ApiException CreateApiException(int statusCode, int? retryAfterSeconds = null)
    {
        var exception = new ApiException($"HTTP {statusCode}")
        {
            ResponseStatusCode = statusCode,
        };

        if (retryAfterSeconds is not null)
        {
            exception.ResponseHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Retry-After"] = [retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture)],
            };
        }

        return exception;
    }

    private sealed class StubRequestAdapter : IRequestAdapter
    {
        private readonly List<ResponseRule> sendAsyncRules = [];
        private readonly Dictionary<string, int> callCounts = new(StringComparer.OrdinalIgnoreCase);

        public string? BaseUrl { get; set; } = "https://graph.microsoft.com/beta";

        public ISerializationWriterFactory SerializationWriterFactory { get; } = SerializationWriterFactoryRegistry.DefaultInstance;

        public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
        {
            _ = backingStoreFactory;
        }

        public Task<T?> SendAsync<T>(
            RequestInformation requestInfo,
            ParsableFactory<T> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            where T : IParsable
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matchingRule = sendAsyncRules.FirstOrDefault(rule =>
                rule.ResponseType == typeof(T) &&
                rule.Predicate(requestInfo));

            if (matchingRule is null)
            {
                throw new InvalidOperationException($"No mocked response configured for '{typeof(T).Name}' and request '{requestInfo.URI}'.");
            }

            callCounts[matchingRule.Key] = callCounts.GetValueOrDefault(matchingRule.Key) + 1;
            if (matchingRule.Results.Count == 0)
            {
                throw new InvalidOperationException($"No queued response remaining for rule '{matchingRule.Key}'.");
            }

            var queuedResult = matchingRule.Results.Dequeue();
            if (queuedResult is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((T?)queuedResult);
        }

        public Task<IEnumerable<T>?> SendCollectionAsync<T>(
            RequestInformation requestInfo,
            ParsableFactory<T> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            where T : IParsable
        {
            throw new NotSupportedException("SendCollectionAsync is not used in these tests.");
        }

        public Task<T?> SendPrimitiveAsync<T>(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("SendPrimitiveAsync is not used in these tests.");
        }

        public Task<IEnumerable<T>?> SendPrimitiveCollectionAsync<T>(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("SendPrimitiveCollectionAsync is not used in these tests.");
        }

        public Task SendNoContentAsync(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("SendNoContentAsync is not used in these tests.");
        }

        public Task<T?> ConvertToNativeRequestAsync<T>(RequestInformation requestInfo, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Native request conversion is not used in these tests.");
        }

        public int GetCallCount(string key)
        {
            return callCounts.GetValueOrDefault(key);
        }

        public void QueueSendAsyncResponse<T>(string key, Func<RequestInformation, bool> predicate, params object[] results)
            where T : IParsable
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(results);

            sendAsyncRules.Add(new ResponseRule(typeof(T), key, predicate, new Queue<object>(results)));
        }

        private sealed record ResponseRule(
            Type ResponseType,
            string Key,
            Func<RequestInformation, bool> Predicate,
            Queue<object> Results);
    }
}
