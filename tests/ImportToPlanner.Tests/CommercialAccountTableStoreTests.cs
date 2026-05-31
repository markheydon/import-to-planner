using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using ImportToPlanner.ApiService.Commercial.CommercialAccounts.Storage;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Tests;

public sealed class CommercialAccountTableStoreTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAsync_PersistsCommercialAccountUsingTenantAndUserIdentityKey()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCommercialAccountStore(tableClient);
        var createdUtc = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

        await store.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        var account = await store.GetAsync("tenant-001", "user-001", CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal("tenant-001", account!.TenantId);
        Assert.Equal("user-001", account.UserId);
        Assert.Equal(CommercialAccountStatus.Active, account.Status);
        Assert.Equal(createdUtc, account.CreatedUtc);
        Assert.Equal(1, tableClient.CreateIfNotExistsCallCount);
    }

    [Fact]
    public async Task AppendAsync_PersistsSignInOutcomeAuditEvent_WithStableOutcomeCode()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCommercialAuditStore(tableClient);
        var occurredUtc = new DateTimeOffset(2026, 5, 28, 12, 15, 0, TimeSpan.Zero);

        await store.AppendAsync(
            new AccountAuditEvent(
                "tenant-001",
                "user-001",
                occurredUtc,
                AccountAuditEventType.SignInOutcome,
                "sign_in_allowed",
                occurredUtc.AddMonths(12)),
            CancellationToken.None);

        Assert.Single(tableClient.StoredEntities);
        var storedEntity = Assert.Single(tableClient.StoredEntities.Values);
        Assert.Equal("tenant-001", storedEntity.PartitionKey);
        Assert.Contains("user-001", storedEntity.RowKey, StringComparison.Ordinal);
        Assert.Contains("SignInOutcome", storedEntity.RowKey, StringComparison.Ordinal);
        Assert.Equal("sign_in_allowed", storedEntity.GetString("Outcome"));
        Assert.Equal(1, tableClient.CreateIfNotExistsCallCount);
    }

    private sealed class FakeTableClient : TableClient
    {
        private readonly Dictionary<string, TableEntity> entities = new(StringComparer.Ordinal);

        public int CreateIfNotExistsCallCount { get; private set; }

        public IReadOnlyDictionary<string, TableEntity> StoredEntities => entities;

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            CreateIfNotExistsCallCount++;
            return Task.FromResult(Response.FromValue(new TableItem("CommercialAccounts"), new FakeResponse(204)));
        }

        public override Task<Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity is not ITableEntity tableEntity)
            {
                throw new InvalidOperationException("Only table entities are supported in this test double.");
            }

            entities[BuildKey(tableEntity.PartitionKey, tableEntity.RowKey)] = ConvertToTableEntity(tableEntity);
            return Task.FromResult<Response>(new FakeResponse(204));
        }

        public override Task<Response<T>> GetEntityAsync<T>(
            string partitionKey,
            string rowKey,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            var key = BuildKey(partitionKey, rowKey);
            if (!entities.TryGetValue(key, out var value))
            {
                throw new RequestFailedException(404, "Entity not found.");
            }

            if (typeof(T) != typeof(TableEntity))
            {
                throw new InvalidOperationException("This test double currently supports TableEntity reads only.");
            }

            return Task.FromResult(Response.FromValue((T)(object)value, new FakeResponse(200)));
        }

        private static string BuildKey(string partitionKey, string rowKey)
            => $"{partitionKey}|{rowKey}";

        private static TableEntity ConvertToTableEntity(ITableEntity source)
        {
            var entity = new TableEntity(source.PartitionKey, source.RowKey)
            {
                ETag = source.ETag,
                Timestamp = source.Timestamp,
            };

            if (source is TableEntity tableEntity)
            {
                foreach (var value in tableEntity)
                {
                    entity[value.Key] = value.Value;
                }
            }

            return entity;
        }
    }

    private sealed class FakeResponse(int status) : Response
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
