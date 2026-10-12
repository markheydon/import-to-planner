namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class ProfileCreditSummaryUseCaseSubstitute
{
    public ProfileCreditSummaryUseCaseSubstitute()
    {
        Instance = Substitute.For<IGetProfileCreditSummaryUseCase>();
        Instance.GetAsync(Arg.Any<ProfileCreditSummaryRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => BuildOutcome(callInfo.Arg<ProfileCreditSummaryRequest>()));
    }

    public IGetProfileCreditSummaryUseCase Instance { get; }

    public int RemainingCredits { get; set; } = 25;

    public bool FailClosed { get; set; }

    public bool ComplimentaryUnlimitedAccess { get; set; }

    public ProfileFreeMonthlyLot? FreeMonthlyLot { get; set; }

    public string? LastTenantId { get; private set; }

    public string FailureCode { get; set; } = CommercialCreditFailureCodes.LedgerUnavailable;

    private Task<GetProfileCreditSummaryOutcome> BuildOutcome(ProfileCreditSummaryRequest request)
    {
        LastTenantId = request.TenantId;

        if (FailClosed)
        {
            return Task.FromResult<GetProfileCreditSummaryOutcome>(
                new GetProfileCreditSummaryOutcome.Failed(new CommercialCreditBalanceFailure(FailureCode)));
        }

        return Task.FromResult<GetProfileCreditSummaryOutcome>(
            new GetProfileCreditSummaryOutcome.Succeeded(
                new ProfileCreditSummaryResult(
                    RemainingCredits,
                    ComplimentaryUnlimitedAccess,
                    ComplimentaryUnlimitedAccess ? null : FreeMonthlyLot,
                    [],
                    false,
                    false)));
    }
}
