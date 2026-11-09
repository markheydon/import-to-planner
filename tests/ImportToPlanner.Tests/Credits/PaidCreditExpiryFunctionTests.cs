using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Functions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImportToPlanner.Tests.Credits;

public sealed class PaidCreditExpiryFunctionTests
{
    [Fact]
    public async Task RunAsync_InvokesExpirePaidCreditLotsUseCaseWithClockUtcNow()
    {
        var asOfUtc = new DateTimeOffset(2026, 10, 13, 3, 0, 0, TimeSpan.Zero);
        var useCase = Substitute.For<IExpirePaidCreditLotsUseCase>();
        useCase
            .ExecuteAsync(asOfUtc, Arg.Any<CancellationToken>())
            .Returns(new ExpirePaidCreditLotsResult(2, 0));

        var function = new PaidCreditExpiryFunction(
            useCase,
            new FixedUtcClock(asOfUtc),
            NullLogger<PaidCreditExpiryFunction>.Instance);

        await function.RunAsync(CancellationToken.None);

        await useCase.Received(1).ExecuteAsync(asOfUtc, Arg.Any<CancellationToken>());
    }

    private sealed class FixedUtcClock(DateTimeOffset utcNow) : IUtcClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
