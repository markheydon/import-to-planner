using ImportToPlanner.Domain;

namespace ImportToPlanner.Tests;

public sealed class PlannerPlanTests
{
    [Fact]
    public void PlannerPlan_ContainsOnlyNeutralDomainProperties()
    {
        // Arrange
        var plan = new PlannerPlan("plan-1", "Team Plan", "container-1", ContainerType.Group);

        // Act + Assert
        Assert.Equal("plan-1", plan.Id);
        Assert.Equal("Team Plan", plan.Title);
        Assert.Equal("container-1", plan.ContainerId);
        Assert.Equal(ContainerType.Group, plan.ContainerType);
    }
}
