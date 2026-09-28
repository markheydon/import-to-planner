namespace ImportToPlanner.Web.Infrastructure;

internal static class E2ETestingHostEnvironment
{
    public const string EnvironmentName = "E2ETesting";

    public static bool IsE2ETesting(IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        return string.Equals(hostEnvironment.EnvironmentName, EnvironmentName, StringComparison.OrdinalIgnoreCase);
    }
}
