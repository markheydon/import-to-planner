using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;

namespace ImportToPlanner.Commercial.Tests;

public sealed class CommercialTableStorageReliabilityTests
{
    [Fact]
    public async Task ListExpiredDeletedAsync_QueriesPersistedStatusValue()
    {
        var tableClient = new RecordingTableClient();
        var store = new TestCommercialAccountsService(tableClient);
        var asOfUtc = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

        var expired = await store.ListExpiredDeletedAsync(asOfUtc, 10, CancellationToken.None);

        Assert.Empty(expired);
        Assert.NotNull(tableClient.LastFilter);
        Assert.Contains("Status eq 'Deleted'", tableClient.LastFilter, StringComparison.Ordinal);
        Assert.Contains("RetentionExpiresUtc le datetime'", tableClient.LastFilter, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureTableAsync_RetriesAfterFailedCreation()
    {
        var tableClient = new RecordingTableClient
        {
            CreateFailures = 1,
        };

        var store = new TestCommercialAccountsService(tableClient);

        await Assert.ThrowsAsync<RequestFailedException>(
            () => store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));

        var expired = await store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None);

        Assert.Empty(expired);
        Assert.Equal(2, tableClient.CreateIfNotExistsCallCount);
    }

    [Fact]
    public async Task EnsureTableAsync_RetriesAfterCancelledCreation()
    {
        using var cancellation = new CancellationTokenSource();
        var tableClient = new RecordingTableClient
        {
            OnCreate = () => cancellation.Cancel(),
        };

        var store = new TestCommercialAccountsService(tableClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, cancellation.Token));

        tableClient.OnCreate = null;

        var expired = await store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None);

        Assert.Empty(expired);
        Assert.Equal(2, tableClient.CreateIfNotExistsCallCount);
    }

    [Fact]
    public async Task EnsureTableAsync_CreatesTableOnceForConcurrentCallers()
    {
        using var releaseCreate = new SemaphoreSlim(0, 1);
        var tableClient = new RecordingTableClient
        {
            OnCreate = () => releaseCreate.Wait(TimeSpan.FromSeconds(5)),
        };

        var store = new TestCommercialAccountsService(tableClient);

        var first = Task.Run(() => store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
        var second = Task.Run(() => store.ListExpiredDeletedAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));

        releaseCreate.Release();
        await Task.WhenAll(first, second);

        Assert.Equal(1, tableClient.CreateIfNotExistsCallCount);
    }

    private sealed class TestCommercialAccountsService(TableClient tableClient)
        : CommercialAccountsService(tableClient);

    private sealed class RecordingTableClient : TableClient
    {
        public int CreateIfNotExistsCallCount { get; private set; }

        public int CreateFailures { get; set; }

        public Action? OnCreate { get; set; }

        public string? LastFilter { get; private set; }

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            CreateIfNotExistsCallCount++;
            OnCreate?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            if (CreateFailures > 0)
            {
                CreateFailures--;
                throw new RequestFailedException(503, "Service unavailable.");
            }

            return Task.FromResult(Response.FromValue(new TableItem("CommercialAccounts"), new EmptyResponse(204)));
        }

        public override AsyncPageable<T> QueryAsync<T>(
            string? filter = null,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return AsyncPageable<T>.FromPages([Page<T>.FromValues([], continuationToken: null, new EmptyResponse(200))]);
        }
    }

    private sealed class EmptyResponse(int status) : Response
    {
        public override int Status => status;

        public override string ReasonPhrase => string.Empty;

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = [];
            return false;
        }
    }
}
