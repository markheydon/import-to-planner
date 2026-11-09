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
    private const string PaidLotRowKeyPrefix = "lot|paid|";
    private const string PaidLotRowKeyUpperBound = "lot|paid|~";
    private const string TransactionRowKeyPrefix = "tx|";
    private const string TransactionRowKeyUpperBound = "tx|~";
    private const string GrantRowKeyPrefix = "grant|";
    private const string UsageIdempotencyRowKeyPrefix = "usage|";
    private const string PaymentIdempotencyRowKeyPrefix = "pay|";
    private const string EventIdempotencyRowKeyPrefix = "evt|";
    private const string AdjustmentIdempotencyRowKeyPrefix = "adj|";

    private readonly TableClient tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    private readonly SemaphoreSlim initialiseSemaphore = new(1, 1);
    private volatile bool tableCreated;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CreditLedgerTransaction>> ListTransactionsAsync(
        string tenantId,
        DateTimeOffset occurredSinceUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var transactions = new List<CreditLedgerTransaction>();
        var transactionPrefix = TransactionRowKeyPrefix;
        var transactionUpperBound = TransactionRowKeyUpperBound;
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
                           filter: TableClient.CreateQueryFilter(
                               $"PartitionKey eq {tenantId} and RowKey ge {transactionPrefix} and RowKey lt {transactionUpperBound}"),
                           cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var transaction = ToTransaction(entity);
            if (transaction.OccurredUtc >= occurredSinceUtc)
            {
                transactions.Add(transaction);
            }
        }

        return transactions;
    }

    /// <inheritdoc/>
    public async Task<bool> HasTransactionsBeforeAsync(
        string tenantId,
        DateTimeOffset beforeOccurredUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var transactionPrefix = TransactionRowKeyPrefix;
        var transactionUpperBound = TransactionRowKeyUpperBound;
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
                           filter: TableClient.CreateQueryFilter(
                               $"PartitionKey eq {tenantId} and RowKey ge {transactionPrefix} and RowKey lt {transactionUpperBound}"),
                           cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var occurredUtc = entity.GetDateTimeOffset("OccurredUtc");
            if (occurredUtc is not null && occurredUtc < beforeOccurredUtc)
            {
                return true;
            }
        }

        return false;
    }

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
                return new RecordCreditUsageOutcome.Success(totalRemaining, lot.LotType == LotType.Paid);
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
    public async Task<RecordCreditUsageOutcome> RecordComplimentaryUsageAsync(
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
            return new RecordCreditUsageOutcome.Success(0, ConsumedPaidCredit: false);
        }

        for (var attempt = 0; attempt < MaxOptimisticConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var complimentaryLot = await GetOrCreateComplimentaryLotAsync(request.TenantId, request.OccurredUtc, cancellationToken)
                .ConfigureAwait(false);
            if (complimentaryLot is null)
            {
                return new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed);
            }

            var usageTransactionId = Guid.NewGuid().ToString("N");
            var grantTransactionId = Guid.NewGuid().ToString("N");
            var usageTransaction = new CreditLedgerTransaction(
                usageTransactionId,
                request.TenantId,
                request.OccurredUtc,
                CreditEntryType.Usage,
                1,
                complimentaryLot.LotId,
                LotType.Complimentary,
                request.ImportRunId,
                request.CreatedPlannerTaskId,
                request.ActorUserId);
            var grantTransaction = new CreditLedgerTransaction(
                grantTransactionId,
                request.TenantId,
                request.OccurredUtc,
                CreditEntryType.ComplimentaryGrant,
                1,
                complimentaryLot.LotId,
                LotType.Complimentary,
                request.ImportRunId,
                request.CreatedPlannerTaskId,
                request.ActorUserId);

            try
            {
                var batch = new List<TableTransactionAction>
                {
                    new(TableTransactionActionType.Add, ToUsageIdempotencyEntity(request, idempotencyRowKey, usageTransactionId)),
                    new(TableTransactionActionType.Add, ToTransactionEntity(usageTransaction)),
                    new(TableTransactionActionType.Add, ToTransactionEntity(grantTransaction)),
                };
                await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                return new RecordCreditUsageOutcome.Success(0, ConsumedPaidCredit: false);
            }
            catch (RequestFailedException exception) when (exception.Status == 409)
            {
                return new RecordCreditUsageOutcome.Success(0, ConsumedPaidCredit: false);
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
    public async Task<CreditLot?> GetPaidLotByExternalPaymentIdAsync(
        string tenantId,
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalPaymentId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
        try
        {
            var response = await tableClient
                .GetEntityAsync<TableEntity>(tenantId, BuildLotRowKey(lotId), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ToLot(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CreditGrantAttemptOutcome> TryGrantPaidPurchaseAsync(
        string tenantId,
        string externalPaymentId,
        string externalEventId,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalPaymentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);
        if (grantQuantity <= 0)
        {
            return new CreditGrantAttemptOutcome.Failure(CommercialCreditFailureCodes.GrantFailed);
        }

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (await HasIdempotencyMarkerAsync(tenantId, BuildEventIdempotencyRowKey(externalEventId), cancellationToken)
                .ConfigureAwait(false)
            || await HasIdempotencyMarkerAsync(tenantId, BuildPaymentIdempotencyRowKey(externalPaymentId), cancellationToken)
                .ConfigureAwait(false))
        {
            return new CreditGrantAttemptOutcome.AlreadyGranted();
        }

        var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
        var lot = new CreditLot(
            lotId,
            tenantId,
            LotType.Paid,
            grantQuantity,
            grantQuantity,
            grantedAtUtc,
            CommercialCreditPolicy.GetPaidLotExpiresAtUtc(grantedAtUtc),
            externalPaymentId);
        var transaction = new CreditLedgerTransaction(
            Guid.NewGuid().ToString("N"),
            tenantId,
            grantedAtUtc,
            CreditEntryType.PaidPurchase,
            grantQuantity,
            lotId,
            LotType.Paid,
            ActorUserId: actorUserId);

        try
        {
            var batch = new List<TableTransactionAction>
            {
                new(TableTransactionActionType.Add, ToEventIdempotencyEntity(tenantId, externalEventId, grantedAtUtc)),
                new(TableTransactionActionType.Add, ToPaymentIdempotencyEntity(tenantId, externalPaymentId, grantedAtUtc)),
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
    public async Task<IReadOnlyList<CreditLot>> ListDuePaidCreditLotsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var dueLots = new List<CreditLot>();
        var paidLotPrefix = PaidLotRowKeyPrefix;
        var paidLotUpperBound = PaidLotRowKeyUpperBound;
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
                           filter: TableClient.CreateQueryFilter(
                               $"RowKey ge {paidLotPrefix} and RowKey lt {paidLotUpperBound}"),
                           cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var lot = ToLot(entity);
            if (lot.LotType == LotType.Paid
                && lot.RemainingQuantity > 0
                && lot.ExpiresAtUtc <= asOfUtc)
            {
                dueLots.Add(lot);
            }
        }

        return dueLots;
    }

    /// <inheritdoc/>
    public async Task<bool> ExpirePaidLotAsync(
        CreditLot lot,
        DateTimeOffset occurredUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(lot);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (lot.LotType != LotType.Paid || lot.RemainingQuantity <= 0)
        {
            return true;
        }

        var transactionId = Guid.NewGuid().ToString("N");
        var transaction = new CreditLedgerTransaction(
            transactionId,
            lot.TenantId,
            occurredUtc,
            CreditEntryType.PaidExpiry,
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
                var transactionEntity = ToTransactionEntity(transaction with { Quantity = remaining });

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
    public async Task<PaidCreditAdjustmentAttemptOutcome> TryAdjustPaidCreditsAsync(
        string tenantId,
        string externalPaymentId,
        string externalAdjustmentId,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalPaymentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAdjustmentId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (await HasIdempotencyMarkerAsync(tenantId, BuildAdjustmentIdempotencyRowKey(externalAdjustmentId), cancellationToken)
                .ConfigureAwait(false))
        {
            return new PaidCreditAdjustmentAttemptOutcome.AlreadyAdjusted();
        }

        var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
        for (var attempt = 0; attempt < MaxOptimisticConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var lotEntityResponse = await tableClient
                    .GetEntityAsync<TableEntity>(tenantId, BuildLotRowKey(lotId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var lotEntity = CloneTableEntity(lotEntityResponse.Value);
                var remaining = lotEntity.GetInt32("RemainingQuantity") ?? 0;
                var quantityToRemove = Math.Max(0, remaining);
                lotEntity["RemainingQuantity"] = remaining - quantityToRemove;

                var transaction = new CreditLedgerTransaction(
                    Guid.NewGuid().ToString("N"),
                    tenantId,
                    occurredUtc,
                    CreditEntryType.PaidAdjustment,
                    quantityToRemove,
                    lotId,
                    LotType.Paid,
                    ActorUserId: null);

                var batch = new List<TableTransactionAction>
                {
                    new(TableTransactionActionType.Add, ToAdjustmentIdempotencyEntity(tenantId, externalAdjustmentId, occurredUtc)),
                    new(TableTransactionActionType.UpdateReplace, lotEntity, lotEntity.ETag),
                    new(TableTransactionActionType.Add, ToTransactionEntity(transaction)),
                };
                await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                return new PaidCreditAdjustmentAttemptOutcome.Applied();
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.PaidLotNotFound);
            }
            catch (RequestFailedException exception) when (exception.Status == 409)
            {
                return new PaidCreditAdjustmentAttemptOutcome.AlreadyAdjusted();
            }
            catch (RequestFailedException exception) when (exception.Status == 412)
            {
                if (attempt == MaxOptimisticConcurrencyRetries - 1)
                {
                    return new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
                }
            }
            catch (RequestFailedException)
            {
                return new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
            }
        }

        return new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
    }

    /// <inheritdoc/>
    public void Dispose() => initialiseSemaphore.Dispose();

    private async Task<CreditLot?> GetOrCreateComplimentaryLotAsync(
        string tenantId,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        var lotId = ComplimentaryCreditConstants.ComplimentaryLotId;
        var rowKey = BuildLotRowKey(lotId);

        try
        {
            var response = await tableClient
                .GetEntityAsync<TableEntity>(tenantId, rowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ToLot(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            var lot = new CreditLot(
                lotId,
                tenantId,
                LotType.Complimentary,
                0,
                0,
                occurredUtc,
                DateTimeOffset.MaxValue);

            try
            {
                await tableClient.AddEntityAsync(ToLotEntity(lot), cancellationToken).ConfigureAwait(false);
                return lot;
            }
            catch (RequestFailedException addException) when (addException.Status == 409)
            {
                var response = await tableClient
                    .GetEntityAsync<TableEntity>(tenantId, rowKey, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return ToLot(response.Value);
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }
        catch (RequestFailedException)
        {
            return null;
        }
    }

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
        return new RecordCreditUsageOutcome.Success(totalRemaining, ConsumedPaidCredit: false);
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
        var entity = new TableEntity(lot.TenantId, BuildLotRowKey(lot.LotId))
        {
            ["LotId"] = lot.LotId,
            ["LotType"] = (int)lot.LotType,
            ["GrantedQuantity"] = lot.GrantedQuantity,
            ["RemainingQuantity"] = lot.RemainingQuantity,
            ["GrantedAtUtc"] = lot.GrantedAtUtc,
            ["ExpiresAtUtc"] = lot.ExpiresAtUtc,
        };

        if (!string.IsNullOrWhiteSpace(lot.ExternalPaymentId))
        {
            entity["ExternalPaymentId"] = lot.ExternalPaymentId;
        }

        return entity;
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
            entity.GetDateTimeOffset("ExpiresAtUtc") ?? DateTimeOffset.UtcNow,
            entity.GetString("ExternalPaymentId"));
    }

    private async Task<bool> HasIdempotencyMarkerAsync(
        string tenantId,
        string rowKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await tableClient
                .GetEntityAsync<TableEntity>(tenantId, rowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }

    private static string BuildPaymentIdempotencyRowKey(string externalPaymentId)
        => $"{PaymentIdempotencyRowKeyPrefix}{externalPaymentId}";

    private static string BuildEventIdempotencyRowKey(string externalEventId)
        => $"{EventIdempotencyRowKeyPrefix}{externalEventId}";

    private static string BuildAdjustmentIdempotencyRowKey(string externalAdjustmentId)
        => $"{AdjustmentIdempotencyRowKeyPrefix}{externalAdjustmentId}";

    private static TableEntity ToPaymentIdempotencyEntity(
        string tenantId,
        string externalPaymentId,
        DateTimeOffset occurredUtc)
    {
        return new TableEntity(tenantId, BuildPaymentIdempotencyRowKey(externalPaymentId))
        {
            ["ExternalPaymentId"] = externalPaymentId,
            ["OccurredUtc"] = occurredUtc,
        };
    }

    private static TableEntity ToEventIdempotencyEntity(
        string tenantId,
        string externalEventId,
        DateTimeOffset occurredUtc)
    {
        return new TableEntity(tenantId, BuildEventIdempotencyRowKey(externalEventId))
        {
            ["ExternalEventId"] = externalEventId,
            ["OccurredUtc"] = occurredUtc,
        };
    }

    private static TableEntity ToAdjustmentIdempotencyEntity(
        string tenantId,
        string externalAdjustmentId,
        DateTimeOffset occurredUtc)
    {
        return new TableEntity(tenantId, BuildAdjustmentIdempotencyRowKey(externalAdjustmentId))
        {
            ["ExternalAdjustmentId"] = externalAdjustmentId,
            ["OccurredUtc"] = occurredUtc,
        };
    }

    private static CreditLedgerTransaction ToTransaction(TableEntity entity)
    {
        return new CreditLedgerTransaction(
            entity.GetString("TransactionId") ?? entity.RowKey.Split('|', 3)[^1],
            entity.PartitionKey,
            entity.GetDateTimeOffset("OccurredUtc") ?? DateTimeOffset.UtcNow,
            (CreditEntryType)(entity.GetInt32("EntryType") ?? (int)CreditEntryType.Usage),
            entity.GetInt32("Quantity") ?? 0,
            entity.GetString("LotId") ?? string.Empty,
            (LotType)(entity.GetInt32("LotType") ?? (int)LotType.FreeMonthly),
            entity.GetString("ImportRunId"),
            entity.GetString("CreatedPlannerTaskId"),
            entity.GetString("ActorUserId"));
    }
}
