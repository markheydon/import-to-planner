using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class ReleaseInformationQueryStub : IReleaseInformationQuery
{
    public ReleaseInformationQueryStub(BuildMetadata? buildMetadata = null)
    {
        ReleaseInformation = new ReleaseInformation(
            "Import To Planner",
            new DeploymentReleaseLabel("v2.3.4-test", false, "2.3.4-test"),
            buildMetadata);
    }

    public ReleaseInformation ReleaseInformation { get; set; }

    public ValueTask<ReleaseInformation> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ReleaseInformation);
}
