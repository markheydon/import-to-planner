using Azure;
using Azure.Data.Tables;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits.Storage;

/// <summary>
/// Azure Table Storage adapter for the commercial credit ledger.
/// </summary>
internal sealed class TableCreditLedgerStore(TableClient tableClient) : ICreditLedgerStore, IDisposable
{
    private const int MaxOptimisticConcurrencyRetries = 5;
    private const string LotRowKeyPrefix = "lot|";
    private const string LotRowKeyUpperBound = "lot|~";
    private const string TransactionRowKeyPrefix = "tx|";
    private const string GrantRowKeyPrefix = "grant|";
    private const string UsageIdempotencyRowKeyPrefix = "usage|";

    private readonly TableClient tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    private readonly SemaphoreSlim initialiseSemaphore = new(1, 1);
    private volatile bool tableCreated;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var lots = new List<CreditLot>();
        var lotPrefix = LotRowKeyPrefix;
        var lotUpperBound = LotRowKeyUpperBound;
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
                           filter: TableClient.CreateQueryFilter(
                               $"PartitionKey eq {tenantId} and RowKey ge {lotPrefix} and RowKey lt {lotUpperBound}"),
                           cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            lots.Add(ToLot(entity));
        }

        return lots;
    }

    /// <inheritdoc/>
    public async Task<bool> HasMonthGrantMarkerAsync(
        string tenantId,
        string utcYearMonth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(utcYearMonth);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await tableClient
                .GetEntityAsync<TableEntity>(tenantId, BuildGrantRowKey(utcYearMonth), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExpireFreeLotAsync(
        CreditLot lot,
        DateTimeOffset occurredUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(lot);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (lot.RemainingQuantity <= 0)
        {
            return true;
        }

        var transactionId = Guid.NewGuid().ToString("N");
        var transaction = new CreditLedgerTransaction(
            transactionId,
            lot.TenantId,
            occurredUtc,
            CreditEntryType.FreeExpiry,
            lot.RemainingQuantity,
            lot.LotId,
            lot.LotType,
            ActorUserId: actorUserId);

        for (var attempt = 0; attempt < MaxOptimisticConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var lotEntityResponse = await tableClient
                    .GetEntityAsync<TableEntity>(lot.TenantId, BuildLotRowKey(lot.LotId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var lotEntity = CloneTableEntity(lotEntityResponse.Value);
                var remaining = lotEntity.GetInt32("RemainingQuantity") ?? 0;
                if (remaining <= 0)
                {
                    return true;
                }

                lotEntity["RemainingQuantity"] = 0;
                var transactionEntity = ToTransactionEntity(transaction);

                var batch = new List<TableTransactionAction>
                {
                    new(TableTransactionActionType.UpdateReplace, lotEntity, lotEntity.ETag),
                    new(TableTransactionActionType.Add, transactionEntity),
                };
                await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (RequestFailedException exception) when (exception.Status == 412)
            {
                if (attempt == MaxOptimisticConcurrencyRetries - 1)
                {
                    return false;
                }
            }
            catch (RequestFailedException)
            {
                return false;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
        string tenantId,
        string utcYearMonth,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(utcYearMonth);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var lotId = Guid.NewGuid().ToString("N");
        var marker = new CreditMonthGrantMarker(tenantId, utcYearMonth, grantedAtUtc, lotId);
        var lot = new CreditLot(
            lotId,
            tenantId,
            LotType.FreeMonthly,
            grantQuantity,
            grantQuantity,
            grantedAtUtc,
            CommercialCreditPolicy.GetFreeLotExpiresAtUtc(grantedAtUtc));
        var transaction = new CreditLedgerTransaction(
            Guid.NewGuid().ToString("N"),
            tenantId,
            grantedAtUtc,
            CreditEntryType.FreeGrant,
            grantQuantity,
            lotId,
            LotType.FreeMonthly,
            ActorUserId: actorUserId);

        try
        {
            var batch = new List<TableTransactionAction>
            {
                new(TableTransactionActionType.Add, ToGrantMarkerEntity(marker)),
                new(TableTransactionActionType.Add, ToLotEntity(lot)),
                new(TableTransactionActionType.Add, ToTransactionEntity(transaction)),
            };
            await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
            return new CreditGrantAttemptOutcome.Applied();
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return new CreditGrantAttemptOutcome.AlreadyGranted();
        }
        catch (RequestFailedException)
        {
            return new CreditGrantAttemptOutcome.Failure(CommercialCreditFailureCodes.GrantFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<RecordCreditUsageOutcome> RecordUsageAsync(
        RecordCreditUsageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ImportRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CreatedPlannerTaskId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var idempotencyRowKey = BuildUsageIdempotencyRowKey(request.ImportRunId, request.CreatedPlannerTaskId);
        if (await HasUsageIdempotencyMarkerAsync(request.TenantId, idempotencyRowKey, cancellationToken).ConfigureAwait(false))
        {
            return await BuildIdempotentUsageSuccessAsync(request, cancellationToken).ConfigureAwait(false);
        }

        for (var attempt = 0; attempt < MaxOptimisticConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lots = await GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
            var lot = CreditLotSelector.SelectConsumableLot(lots, request.OccurredUtc);

            if (lot is null)
            {
                return new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted);
            }

            var transactionId = Guid.NewGuid().ToString("N");
            var transaction = new CreditLedgerTransaction(
                transactionId,
                request.TenantId,
                request.OccurredUtc,
                CreditEntryType.Usage,
                1,
                lot.LotId,
                lot.LotType,
                request.ImportRunId,
                request.CreatedPlannerTaskId,
                request.ActorUserId);

            try
            {
                var lotEntityResponse = await tableClient
                    .GetEntityAsync<TableEntity>(request.TenantId, BuildLotRowKey(lot.LotId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var lotEntity = CloneTableEntity(lotEntityResponse.Value);
                var remaining = lotEntity.GetInt32("RemainingQuantity") ?? 0;
                if (remaining <= 0 || !CreditLotSelector.IsConsumable(lot, request.OccurredUtc))
                {
                    continue;
                }

                lotEntity["RemainingQuantity"] = remaining - 1;
                var batch = new List<TableTransactionAction>
                {
                    new(TableTransactionActionType.Add, ToUsageIdempotencyEntity(request, idempotencyRowKey, transactionId)),
                    new(TableTransactionActionType.UpdateReplace, lotEntity, lotEntity.ETag),
                    new(TableTransactionActionType.Add, ToTransactionEntity(transaction)),
                };
                await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);

                var refreshedLots = await GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
                var totalRemaining = CreditLotSelector.SumConsumableRemaining(refreshedLots, request.OccurredUtc);
                return new RecordCreditUsageOutcome.Success(totalRemaining);
            }
            catch (RequestFailedException exception) when (exception.Status == 409)
            {
                return await BuildIdempotentUsageSuccessAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when (exception.Status == 412)
            {
                if (attempt == MaxOptimisticConcurrencyRetries - 1)
                {
                    return new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed);
                }
            }
            catch (RequestFailedException)
            {
                return new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed);
            }
        }

        return new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed);
    }

    /// <inheritdoc/>
    public void Dispose() => initialiseSemaphore.Dispose();

    private async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        if (tableCreated)
        {
            return;
        }

        await initialiseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (tableCreated)
            {
                return;
            }

            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            tableCreated = true;
        }
        finally
        {
            initialiseSemaphore.Release();
        }
    }

    private static TableEntity CloneTableEntity(TableEntity source)
    {
        var clone = new TableEntity(source.PartitionKey, source.RowKey)
        {
            ETag = source.ETag,
            Timestamp = source.Timestamp,
        };

        foreach (var property in source)
        {
            clone[property.Key] = property.Value;
        }

        return clone;
    }

    private async Task<bool> HasUsageIdempotencyMarkerAsync(
        string tenantId,
        string idempotencyRowKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await tableClient
                .GetEntityAsync<TableEntity>(tenantId, idempotencyRowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }

    private async Task<RecordCreditUsageOutcome> BuildIdempotentUsageSuccessAsync(
        RecordCreditUsageRequest request,
        CancellationToken cancellationToken)
    {
        var lots = await GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        var totalRemaining = CreditLotSelector.SumConsumableRemaining(lots, request.OccurredUtc);
        return new RecordCreditUsageOutcome.Success(totalRemaining);
    }

    private static string BuildLotRowKey(string lotId) => $"{LotRowKeyPrefix}{lotId}";

    private static string BuildGrantRowKey(string utcYearMonth) => $"{GrantRowKeyPrefix}{utcYearMonth}";

    private static string BuildUsageIdempotencyRowKey(string importRunId, string createdPlannerTaskId)
        => $"{UsageIdempotencyRowKeyPrefix}{importRunId}|{createdPlannerTaskId}";

    private static string BuildTransactionRowKey(DateTimeOffset occurredUtc, string transactionId)
    {
        var reverseTicks = long.MaxValue - occurredUtc.UtcTicks;
        return $"{TransactionRowKeyPrefix}{reverseTicks:D19}|{transactionId}";
    }

    private static TableEntity ToLotEntity(CreditLot lot)
    {
        return new TableEntity(lot.TenantId, BuildLotRowKey(lot.LotId))
        {
            ["LotId"] = lot.LotId,
            ["LotType"] = (int)lot.LotType,
            ["GrantedQuantity"] = lot.GrantedQuantity,
            ["RemainingQuantity"] = lot.RemainingQuantity,
            ["GrantedAtUtc"] = lot.GrantedAtUtc,
            ["ExpiresAtUtc"] = lot.ExpiresAtUtc,
        };
    }

    private static TableEntity ToUsageIdempotencyEntity(
        RecordCreditUsageRequest request,
        string idempotencyRowKey,
        string transactionId)
    {
        return new TableEntity(request.TenantId, idempotencyRowKey)
        {
            ["ImportRunId"] = request.ImportRunId,
            ["CreatedPlannerTaskId"] = request.CreatedPlannerTaskId,
            ["TransactionId"] = transactionId,
            ["OccurredUtc"] = request.OccurredUtc,
        };
    }

    private static TableEntity ToGrantMarkerEntity(CreditMonthGrantMarker marker)
    {
        return new TableEntity(marker.TenantId, BuildGrantRowKey(marker.UtcYearMonth))
        {
            ["UtcYearMonth"] = marker.UtcYearMonth,
            ["GrantedAtUtc"] = marker.GrantedAtUtc,
            ["LotId"] = marker.LotId,
        };
    }

    private static TableEntity ToTransactionEntity(CreditLedgerTransaction transaction)
    {
        var entity = new TableEntity(transaction.TenantId, BuildTransactionRowKey(transaction.OccurredUtc, transaction.TransactionId))
        {
            ["TransactionId"] = transaction.TransactionId,
            ["OccurredUtc"] = transaction.OccurredUtc,
            ["EntryType"] = (int)transaction.EntryType,
            ["Quantity"] = transaction.Quantity,
            ["LotId"] = transaction.LotId,
            ["LotType"] = (int)transaction.LotType,
        };

        if (!string.IsNullOrWhiteSpace(transaction.ImportRunId))
        {
            entity["ImportRunId"] = transaction.ImportRunId;
        }

        if (!string.IsNullOrWhiteSpace(transaction.CreatedPlannerTaskId))
        {
            entity["CreatedPlannerTaskId"] = transaction.CreatedPlannerTaskId;
        }

        if (!string.IsNullOrWhiteSpace(transaction.ActorUserId))
        {
            entity["ActorUserId"] = transaction.ActorUserId;
        }

        return entity;
    }

    private static CreditLot ToLot(TableEntity entity)
    {
        return new CreditLot(
            entity.GetString("LotId") ?? entity.RowKey[LotRowKeyPrefix.Length..],
            entity.PartitionKey,
            (LotType)(entity.GetInt32("LotType") ?? (int)LotType.FreeMonthly),
            entity.GetInt32("GrantedQuantity") ?? 0,
            entity.GetInt32("RemainingQuantity") ?? 0,
            entity.GetDateTimeOffset("GrantedAtUtc") ?? DateTimeOffset.UtcNow,
            entity.GetDateTimeOffset("ExpiresAtUtc") ?? DateTimeOffset.UtcNow);
    }
}
