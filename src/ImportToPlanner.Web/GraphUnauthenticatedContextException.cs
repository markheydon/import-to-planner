namespace ImportToPlanner.Web;

/// <summary>
/// Thrown when a Graph token cannot be acquired because there is no authenticated user context.
/// </summary>
internal sealed class GraphUnauthenticatedContextException : InvalidOperationException
{
    internal GraphUnauthenticatedContextException()
        : base("An authenticated user context is required to acquire a Graph access token.")
    {
    }
}
