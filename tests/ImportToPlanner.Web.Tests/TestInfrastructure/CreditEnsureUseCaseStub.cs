namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CreditEnsureUseCaseStub : IEnsureCurrentCreditBalanceUseCase
{
    public int RemainingCredits { get; set; } = 25;

    public bool FailClosed { get; set; }

    public string FailureCode { get; set; } = CommercialCreditFailureCodes.LedgerUnavailable;

    public int EnsureCallCount { get; private set; }

    public EnsureBalanceReason? LastReason { get; private set; }

    public Task<EnsureCurrentCreditBalanceOutcome> EnsureAsync(
        EnsureCurrentCreditBalanceRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCallCount++;
        LastReason = request.Reason;
        cancellationToken.ThrowIfCancellationRequested();

        if (FailClosed)
        {
            return Task.FromResult<EnsureCurrentCreditBalanceOutcome>(
                new EnsureCurrentCreditBalanceOutcome.Failed(new CommercialCreditBalanceFailure(FailureCode)));
        }

        return Task.FromResult<EnsureCurrentCreditBalanceOutcome>(
            new EnsureCurrentCreditBalanceOutcome.Succeeded(
                CommercialCreditBalanceResult.Success(RemainingCredits, RemainingCredits, 0, false, false)));
    }
}
