using System.Text.RegularExpressions;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using ImportToPlanner.Commercial.Credits.Storage;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Tests.Credits;

public sealed class CreditLedgerTableStoreTests
{
    [Fact]
    public async Task TryGrantFreeMonthlyAsync_FirstCall_PersistsGrantMarkerLotAndTransaction()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var outcome = await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        Assert.IsType<CreditGrantAttemptOutcome.Applied>(outcome);
        Assert.True(await store.HasMonthGrantMarkerAsync("tenant-001", "202609", CancellationToken.None));

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        var lot = Assert.Single(lots);
        Assert.Equal(25, lot.GrantedQuantity);
        Assert.Equal(25, lot.RemainingQuantity);
        Assert.Equal(LotType.FreeMonthly, lot.LotType);
        Assert.Contains(tableClient.StoredEntities.Values, entity => entity.RowKey.StartsWith("grant|", StringComparison.Ordinal));
        Assert.Contains(tableClient.StoredEntities.Values, entity => entity.RowKey.StartsWith("tx|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryGrantFreeMonthlyAsync_SecondCallSameMonth_ReturnsAlreadyGranted()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var secondOutcome = await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc.AddHours(1),
            "user-001",
            CancellationToken.None);

        Assert.IsType<CreditGrantAttemptOutcome.AlreadyGranted>(secondOutcome);
        Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None));
    }

    [Fact]
    public async Task RecordUsageAsync_AfterGrant_DecrementsLotRemainingInTransaction()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var usageOutcome = await store.RecordUsageAsync(
            new RecordCreditUsageRequest("tenant-001", "user-001", grantedAtUtc.AddMinutes(5), "run-1", "task-1"),
            CancellationToken.None);

        var success = Assert.IsType<RecordCreditUsageOutcome.Success>(usageOutcome);
        Assert.Equal(24, success.RemainingCredits);

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.Equal(24, Assert.Single(lots).RemainingQuantity);
        Assert.Contains(tableClient.StoredEntities.Values, entity =>
            entity.RowKey.StartsWith("tx|", StringComparison.Ordinal)
            && entity.GetInt32("EntryType") == (int)CreditEntryType.Usage);
    }

    [Fact]
    public async Task TryGrantFreeMonthlyAsync_WhenGrantBatchFails_LeavesNoGrantMarkerOrLot()
    {
        var tableClient = new FailingGrantBatchFakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var outcome = await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        Assert.IsType<CreditGrantAttemptOutcome.Failure>(outcome);
        Assert.False(await store.HasMonthGrantMarkerAsync("tenant-001", "202609", CancellationToken.None));
        Assert.Empty(await store.GetLotsAsync("tenant-001", CancellationToken.None));
        Assert.DoesNotContain(tableClient.StoredEntities.Values, entity => entity.RowKey.StartsWith("grant|", StringComparison.Ordinal));
        Assert.DoesNotContain(tableClient.StoredEntities.Values, entity => entity.RowKey.StartsWith("lot|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecordUsageAsync_WhenFirstTransactionConflictFails_RetriesAndSucceeds()
    {
        var tableClient = new FakeTableClient();
        var failedOnce = false;
        tableClient.FailUpdateReplaceOnce = actions =>
        {
            if (failedOnce || !actions.Any(action => action.ActionType == TableTransactionActionType.UpdateReplace))
            {
                return false;
            }

            failedOnce = true;
            return true;
        };
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var usageOutcome = await store.RecordUsageAsync(
            new RecordCreditUsageRequest("tenant-001", "user-001", grantedAtUtc.AddMinutes(5), "run-1", "task-1"),
            CancellationToken.None);

        var success = Assert.IsType<RecordCreditUsageOutcome.Success>(usageOutcome);
        Assert.True(failedOnce);
        Assert.Equal(1, tableClient.UpdateReplaceExecutions);
        Assert.Equal(24, success.RemainingCredits);
        Assert.Equal(24, Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None)).RemainingQuantity);
    }

    [Fact]
    public async Task ExpireFreeLotAsync_WhenFirstTransactionConflictFails_RetriesAndSucceeds()
    {
        var tableClient = new FakeTableClient();
        var failedOnce = false;
        tableClient.FailUpdateReplaceOnce = actions =>
        {
            if (failedOnce || !actions.Any(action => action.ActionType == TableTransactionActionType.UpdateReplace))
            {
                return false;
            }

            failedOnce = true;
            return true;
        };
        var store = new TableCreditLedgerStore(tableClient);
        var grantedAtUtc = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);

        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202608",
            25,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        var lot = Assert.Single(lots);
        var expired = await store.ExpireFreeLotAsync(
            lot,
            new DateTimeOffset(2026, 9, 1, 0, 5, 0, TimeSpan.Zero),
            "user-001",
            CancellationToken.None);

        Assert.True(expired);
        Assert.True(failedOnce);
        Assert.Equal(0, Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None)).RemainingQuantity);
        Assert.Contains(tableClient.StoredEntities.Values, entity =>
            entity.RowKey.StartsWith("tx|", StringComparison.Ordinal)
            && entity.GetInt32("EntryType") == (int)CreditEntryType.FreeExpiry);
    }

    [Fact]
    public async Task RecordUsageAsync_WithMixedLots_DebitsFreeBeforeOldestPaid()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var olderPaidGrantedAt = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var newerPaidGrantedAt = new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            1,
            occurredUtc,
            "user-001",
            CancellationToken.None);

        var olderPaidLotId = Guid.NewGuid().ToString("N");
        var newerPaidLotId = Guid.NewGuid().ToString("N");
        await tableClient.AddEntityAsync(CreateLotEntity(
            "tenant-001",
            olderPaidLotId,
            LotType.Paid,
            1,
            olderPaidGrantedAt,
            occurredUtc.AddYears(1)), CancellationToken.None);
        await tableClient.AddEntityAsync(CreateLotEntity(
            "tenant-001",
            newerPaidLotId,
            LotType.Paid,
            1,
            newerPaidGrantedAt,
            occurredUtc.AddYears(1)), CancellationToken.None);

        var firstUsage = await store.RecordUsageAsync(
            new RecordCreditUsageRequest("tenant-001", "user-001", occurredUtc.AddMinutes(1), "run-1", "task-1"),
            CancellationToken.None);
        Assert.IsType<RecordCreditUsageOutcome.Success>(firstUsage);

        var lotsAfterFirst = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        var freeLot = lotsAfterFirst.Single(lot => lot.LotType == LotType.FreeMonthly);
        Assert.Equal(0, freeLot.RemainingQuantity);

        var secondUsage = await store.RecordUsageAsync(
            new RecordCreditUsageRequest("tenant-001", "user-001", occurredUtc.AddMinutes(2), "run-1", "task-2"),
            CancellationToken.None);
        Assert.IsType<RecordCreditUsageOutcome.Success>(secondUsage);

        var lotsAfterSecond = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.Equal(0, lotsAfterSecond.Single(lot => lot.LotId == olderPaidLotId).RemainingQuantity);
        Assert.Equal(1, lotsAfterSecond.Single(lot => lot.LotId == newerPaidLotId).RemainingQuantity);

        var thirdUsage = await store.RecordUsageAsync(
            new RecordCreditUsageRequest("tenant-001", "user-001", occurredUtc.AddMinutes(3), "run-1", "task-3"),
            CancellationToken.None);
        Assert.IsType<RecordCreditUsageOutcome.Success>(thirdUsage);
        Assert.Equal(0, (await store.GetLotsAsync("tenant-001", CancellationToken.None)).Sum(lot => lot.RemainingQuantity));
    }

    private static TableEntity CreateLotEntity(
        string tenantId,
        string lotId,
        LotType lotType,
        int remainingQuantity,
        DateTimeOffset grantedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new TableEntity(tenantId, $"lot|{lotId}")
        {
            ["LotId"] = lotId,
            ["LotType"] = (int)lotType,
            ["GrantedQuantity"] = remainingQuantity,
            ["RemainingQuantity"] = remainingQuantity,
            ["GrantedAtUtc"] = grantedAtUtc,
            ["ExpiresAtUtc"] = expiresAtUtc,
        };
    }

    private class FakeTableClient : TableClient
    {
        private static readonly Regex PartitionFilterRegex = new(
            @"PartitionKey eq '(?<tenant>[^']+)'",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly Dictionary<string, TableEntity> entities = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, TableEntity> StoredEntities => entities;

        public Func<IReadOnlyList<TableTransactionAction>, bool>? FailUpdateReplaceOnce { get; set; }

        public int UpdateReplaceExecutions { get; private set; }

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Response.FromValue(new TableItem("CommercialCreditLedger"), new FakeResponse(204)));

        public override Task<Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity is not ITableEntity tableEntity)
            {
                throw new InvalidOperationException("Only table entities are supported in this test double.");
            }

            var key = BuildKey(tableEntity.PartitionKey, tableEntity.RowKey);
            if (entities.ContainsKey(key))
            {
                throw new RequestFailedException(409, "Entity already exists.");
            }

            entities[key] = ConvertToTableEntity(tableEntity);
            entities[key].ETag = new ETag(Guid.NewGuid().ToString("N"));
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

            return Task.FromResult(Response.FromValue((T)(object)ConvertToTableEntity(value), new FakeResponse(200)));
        }

        public override Task<Response<IReadOnlyList<Response>>> SubmitTransactionAsync(
            IEnumerable<TableTransactionAction> transactionActions,
            CancellationToken cancellationToken = default)
        {
            var actions = transactionActions.ToList();
            if (FailUpdateReplaceOnce?.Invoke(actions) == true)
            {
                throw new RequestFailedException(412, "Precondition failed.");
            }

            foreach (var action in actions)
            {
                var entity = action.Entity;
                var key = BuildKey(entity.PartitionKey, entity.RowKey);
                switch (action.ActionType)
                {
                    case TableTransactionActionType.Add:
                        if (entities.ContainsKey(key))
                        {
                            throw new RequestFailedException(409, "Entity already exists.");
                        }

                        entities[key] = ConvertToTableEntity(entity);
                        entities[key].ETag = new ETag(Guid.NewGuid().ToString("N"));
                        break;
                    case TableTransactionActionType.UpdateReplace:
                        UpdateReplaceExecutions++;
                        if (!entities.TryGetValue(key, out var existing))
                        {
                            throw new RequestFailedException(404, "Entity not found.");
                        }

                        if (action.ETag != ETag.All && action.ETag != existing.ETag)
                        {
                            throw new RequestFailedException(412, "Precondition failed.");
                        }

                        var updated = ConvertToTableEntity(entity);
                        foreach (var property in updated)
                        {
                            existing[property.Key] = property.Value;
                        }

                        existing.ETag = new ETag(Guid.NewGuid().ToString("N"));
                        entities[key] = existing;
                        break;
                    default:
                        throw new NotSupportedException($"Action type {action.ActionType} is not supported.");
                }
            }

            return Task.FromResult(Response.FromValue<IReadOnlyList<Response>>(
                transactionActions.Select(_ => (Response)new FakeResponse(204)).ToList(),
                new FakeResponse(204)));
        }

        public override AsyncPageable<T> QueryAsync<T>(
            string? filter = null,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            if (typeof(T) != typeof(TableEntity))
            {
                throw new InvalidOperationException("This test double currently supports TableEntity queries only.");
            }

            var tenantId = ExtractTenantId(filter);
            var results = entities.Values
                .Where(entity => entity.PartitionKey == tenantId
                    && entity.RowKey.StartsWith("lot|", StringComparison.Ordinal)
                    && entity.RowKey.CompareTo("lot|~", StringComparison.Ordinal) < 0)
                .Cast<T>()
                .ToList();

            return new FakeAsyncPageable<T>(results);
        }

        private static string? ExtractTenantId(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            var match = PartitionFilterRegex.Match(filter);
            return match.Success ? match.Groups["tenant"].Value : null;
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

    private sealed class FailingGrantBatchFakeTableClient : FakeTableClient
    {
        public override Task<Response<IReadOnlyList<Response>>> SubmitTransactionAsync(
            IEnumerable<TableTransactionAction> transactionActions,
            CancellationToken cancellationToken = default)
            => throw new RequestFailedException(500, "Transaction failed.");
    }

    private sealed class FakeAsyncPageable<T> : AsyncPageable<T>
        where T : notnull
    {
        private readonly IReadOnlyList<T> items;

        public FakeAsyncPageable(IReadOnlyList<T> items) => this.items = items;

        public override IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            return GetPagesAsync();

            async IAsyncEnumerable<Page<T>> GetPagesAsync()
            {
                await Task.CompletedTask;
                yield return Page<T>.FromValues(items, continuationToken: null, new FakeResponse(200));
            }
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
