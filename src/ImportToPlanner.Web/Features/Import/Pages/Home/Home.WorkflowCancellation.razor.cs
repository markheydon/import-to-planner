using Microsoft.AspNetCore.Components;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    [Inject]
    internal IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    /// <summary>
    /// Request-scoped cancellation for workflow I/O. On interactive Blazor Server circuits,
    /// <see cref="HttpContext"/> is often unavailable after the initial HTTP request, so this
    /// falls back to <see cref="CancellationToken.None"/> for user-triggered actions.
    /// </summary>
    private CancellationToken WorkflowCancellation
        => HttpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
}
