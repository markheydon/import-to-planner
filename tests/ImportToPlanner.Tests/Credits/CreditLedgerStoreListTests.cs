using System.Text.RegularExpressions;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using ImportToPlanner.Commercial.Credits.Storage;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Tests.Credits;

public sealed class CreditLedgerStoreListTests
{
    [Fact]
    public async Task ListTransactionsAsync_UsesTenantPartitionAndTransactionRowKeyRange()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var inWindowUtc = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
        await tableClient.AddEntityAsync(CreateTransactionEntity("tenant-001", "tx-in-window", inWindowUtc), CancellationToken.None);
        await tableClient.AddEntityAsync(new TableEntity("tenant-001", "lot|ignored"), CancellationToken.None);
        await tableClient.AddEntityAsync(new TableEntity("tenant-001", "grant|202609"), CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-002", "tx-other-tenant", inWindowUtc),
            CancellationToken.None);

        var transactions = await store.ListTransactionsAsync(
            "tenant-001",
            inWindowUtc.AddDays(-1),
            CancellationToken.None);

        var transaction = Assert.Single(transactions);
        Assert.Equal("tx-in-window", transaction.TransactionId);
        Assert.NotNull(tableClient.LastQueryFilter);
        Assert.Contains("PartitionKey eq", tableClient.LastQueryFilter, StringComparison.Ordinal);
        Assert.Contains("tenant-001", tableClient.LastQueryFilter, StringComparison.Ordinal);
        Assert.Contains("RowKey ge", tableClient.LastQueryFilter, StringComparison.Ordinal);
        Assert.Contains("RowKey lt", tableClient.LastQueryFilter, StringComparison.Ordinal);
        Assert.Contains("tx|", tableClient.LastQueryFilter, StringComparison.Ordinal);
        Assert.Contains("tx|~", tableClient.LastQueryFilter, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListTransactionsAsync_FiltersOccurredSinceUtc_InclusiveAtBoundary()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var boundaryUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var beforeBoundaryUtc = boundaryUtc.AddMinutes(-1);
        var afterBoundaryUtc = boundaryUtc.AddHours(1);

        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "before-boundary", beforeBoundaryUtc, CreditEntryType.Usage),
            CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "on-boundary", boundaryUtc, CreditEntryType.FreeGrant),
            CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "after-boundary", afterBoundaryUtc, CreditEntryType.Usage),
            CancellationToken.None);

        var transactions = await store.ListTransactionsAsync("tenant-001", boundaryUtc, CancellationToken.None);

        Assert.Equal(2, transactions.Count);
        Assert.Contains(transactions, transaction => transaction.TransactionId == "on-boundary");
        Assert.Contains(transactions, transaction => transaction.TransactionId == "after-boundary");
        Assert.DoesNotContain(transactions, transaction => transaction.TransactionId == "before-boundary");
    }

    [Fact]
    public async Task HasTransactionsBeforeAsync_WhenPreWindowTransactionExists_ReturnsTrue()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var windowStartUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var preWindowUtc = windowStartUtc.AddDays(-30);
        var inWindowUtc = windowStartUtc.AddDays(1);

        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "pre-window", preWindowUtc),
            CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "in-window", inWindowUtc),
            CancellationToken.None);

        var hasOlder = await store.HasTransactionsBeforeAsync("tenant-001", windowStartUtc, CancellationToken.None);

        Assert.True(hasOlder);
    }

    [Fact]
    public async Task HasTransactionsBeforeAsync_WhenNoPreWindowTransactionExists_ReturnsFalse()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var windowStartUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var onWindowUtc = windowStartUtc;
        var afterWindowUtc = windowStartUtc.AddDays(1);

        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "on-window", onWindowUtc),
            CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "after-window", afterWindowUtc),
            CancellationToken.None);

        var hasOlder = await store.HasTransactionsBeforeAsync("tenant-001", windowStartUtc, CancellationToken.None);

        Assert.False(hasOlder);
    }

    [Fact]
    public async Task ListTransactionsAsync_ReturnsReverseChronologicalOrderByRowKeyScan()
    {
        var tableClient = new FakeTableClient();
        var store = new TableCreditLedgerStore(tableClient);
        var olderUtc = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var newerUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var boundaryUtc = olderUtc.AddDays(-1);

        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "older", olderUtc),
            CancellationToken.None);
        await tableClient.AddEntityAsync(
            CreateTransactionEntity("tenant-001", "newer", newerUtc),
            CancellationToken.None);

        var transactions = await store.ListTransactionsAsync("tenant-001", boundaryUtc, CancellationToken.None);

        Assert.Equal(2, transactions.Count);
        Assert.Equal("newer", transactions[0].TransactionId);
        Assert.Equal("older", transactions[1].TransactionId);
        Assert.True(transactions[0].OccurredUtc > transactions[1].OccurredUtc);
    }

    private static TableEntity CreateTransactionEntity(
        string tenantId,
        string transactionId,
        DateTimeOffset occurredUtc,
        CreditEntryType entryType = CreditEntryType.Usage)
    {
        return new TableEntity(tenantId, BuildTransactionRowKey(occurredUtc, transactionId))
        {
            ["TransactionId"] = transactionId,
            ["OccurredUtc"] = occurredUtc,
            ["EntryType"] = (int)entryType,
            ["Quantity"] = 1,
            ["LotId"] = "lot-001",
            ["LotType"] = (int)LotType.FreeMonthly,
        };
    }

    private static string BuildTransactionRowKey(DateTimeOffset occurredUtc, string transactionId)
    {
        var reverseTicks = long.MaxValue - occurredUtc.UtcTicks;
        return $"tx|{reverseTicks:D19}|{transactionId}";
    }

    private sealed class FakeTableClient : TableClient
    {
        private static readonly Regex PartitionFilterRegex = new(
            @"PartitionKey eq '(?<tenant>[^']+)'",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex RowKeyLowerBoundRegex = new(
            @"RowKey ge '(?<lower>[^']*)'",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex RowKeyUpperBoundRegex = new(
            @"RowKey lt '(?<upper>[^']*)'",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly Dictionary<string, TableEntity> entities = new(StringComparer.Ordinal);

        public string? LastQueryFilter { get; private set; }

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

            LastQueryFilter = filter;
            var tenantId = ExtractTenantId(filter);
            var lowerBound = ExtractRowKeyLowerBound(filter) ?? string.Empty;
            var upperBound = ExtractRowKeyUpperBound(filter) ?? "~";
            var results = entities.Values
                .Where(entity => entity.PartitionKey == tenantId
                    && string.Compare(entity.RowKey, lowerBound, StringComparison.Ordinal) >= 0
                    && string.Compare(entity.RowKey, upperBound, StringComparison.Ordinal) < 0)
                .OrderBy(entity => entity.RowKey, StringComparer.Ordinal)
                .Cast<T>()
                .ToList();

            return new FakeAsyncPageable<T>(results);
        }

        private static string? ExtractRowKeyLowerBound(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            var match = RowKeyLowerBoundRegex.Match(filter);
            return match.Success ? match.Groups["lower"].Value : null;
        }

        private static string? ExtractRowKeyUpperBound(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            var match = RowKeyUpperBoundRegex.Match(filter);
            return match.Success ? match.Groups["upper"].Value : null;
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
