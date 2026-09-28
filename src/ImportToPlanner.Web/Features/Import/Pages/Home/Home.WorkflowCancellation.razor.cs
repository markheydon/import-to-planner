using Microsoft.AspNetCore.Components;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    [Inject]
    internal IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    private CancellationToken WorkflowCancellation
        => HttpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
}
