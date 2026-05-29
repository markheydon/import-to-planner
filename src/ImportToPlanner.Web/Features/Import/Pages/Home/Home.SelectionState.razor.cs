namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private bool IsCurrentSelectionInSyncWithRequest()
        => selectedContainer is not null
           && selectedPlan is not null
           && currentRequest is not null
           && string.Equals(selectedContainer.Id, currentRequest.ContainerId, StringComparison.Ordinal)
           && string.Equals(selectedPlan.Id, currentRequest.PlanId, StringComparison.Ordinal);
}
