namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CreditEnsureUseCaseSubstitute
{
    public CreditEnsureUseCaseSubstitute()
    {
        Instance = Substitute.For<IEnsureCurrentCreditBalanceUseCase>();
        Instance.EnsureAsync(Arg.Any<EnsureCurrentCreditBalanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => BuildOutcome(callInfo.Arg<EnsureCurrentCreditBalanceRequest>()));
    }

    public IEnsureCurrentCreditBalanceUseCase Instance { get; }

    public int RemainingCredits { get; set; } = 25;

    public bool FailClosed { get; set; }

    public string FailureCode { get; set; } = CommercialCreditFailureCodes.LedgerUnavailable;

    public int EnsureCallCount { get; private set; }

    public EnsureBalanceReason? LastReason { get; private set; }

    private Task<EnsureCurrentCreditBalanceOutcome> BuildOutcome(EnsureCurrentCreditBalanceRequest request)
    {
        EnsureCallCount++;
        LastReason = request.Reason;

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
