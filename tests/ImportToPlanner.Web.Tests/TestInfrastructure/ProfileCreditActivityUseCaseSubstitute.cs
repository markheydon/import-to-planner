namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class ProfileCreditActivityUseCaseSubstitute
{
    public ProfileCreditActivityUseCaseSubstitute()
    {
        Instance = Substitute.For<IGetProfileCreditActivityUseCase>();
        Instance.GetAsync(Arg.Any<ProfileCreditActivityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => BuildOutcome(callInfo.Arg<ProfileCreditActivityRequest>()));
    }

    public IGetProfileCreditActivityUseCase Instance { get; }

    public bool FailClosed { get; set; }

    public bool HasOlderHistory { get; set; }

    public IReadOnlyList<ProfileCreditActivityEntry> Entries { get; set; } = [];

    private Task<GetProfileCreditActivityOutcome> BuildOutcome(ProfileCreditActivityRequest request)
    {
        if (FailClosed)
        {
            return Task.FromResult<GetProfileCreditActivityOutcome>(
                new GetProfileCreditActivityOutcome.Failed(
                    new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable)));
        }

        return Task.FromResult<GetProfileCreditActivityOutcome>(
            new GetProfileCreditActivityOutcome.Succeeded(
                new ProfileCreditActivityResult(
                    Entries,
                    HasOlderHistory,
                    request.OccurredUtc.AddMonths(-12))));
    }
}
