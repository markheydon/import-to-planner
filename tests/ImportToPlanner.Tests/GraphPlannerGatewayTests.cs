using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Domain;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

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
    public async Task FindPlanByNameAsync_WithNoMatchingPlan_ReturnsNull()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerPlanCollectionResponse>(
            "plans",
            request => request.URI?.AbsolutePath.EndsWith("/planner/plans", StringComparison.OrdinalIgnoreCase) == true,
            new PlannerPlanCollectionResponse
            {
                Value = [new Microsoft.Graph.Models.PlannerPlan { Id = "plan-1", Title = "Other" }],
            });

        var gateway = CreateGateway(adapter);

        // Act
        var result = await gateway.FindPlanByNameAsync("group-1", "Target", CancellationToken.None);

        // Assert
        Assert.Null(result);
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
        adapter.QueueSendAsyncResponse<PlannerTask>(
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
    public async Task CreateTaskAsync_WhenGraphReturnsConflictTwice_ThrowsPlannerConflictException()
    {
        // Arrange
        var adapter = new StubRequestAdapter();
        adapter.QueueSendAsyncResponse<PlannerTask>(
            "createTask",
            request => request.URI?.AbsolutePath.EndsWith("/planner/tasks", StringComparison.OrdinalIgnoreCase) == true,
            CreateApiException(412),
            CreateApiException(412));

        var gateway = CreateGateway(adapter);

        // Act + Assert
        await Assert.ThrowsAsync<PlannerConflictException>(() =>
            gateway.CreateTaskAsync("plan-1", "bucket-1", "Task A", null, null, null, CancellationToken.None));
        Assert.Equal(2, adapter.GetCallCount("createTask"));
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
                ["Retry-After"] = [retryAfterSeconds.Value.ToString()],
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