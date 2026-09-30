using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

internal sealed class ThrowingReleaseInformationQueryStub : IReleaseInformationQuery
{
    public ValueTask<ReleaseInformation> GetAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Simulated release query failure.");
}
