using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Presenters;

/// <summary>
/// Maps neutral planner failures to user-safe messages for web presentation.
/// </summary>
internal static class PlannerFailureMessageMapper
{
    /// <summary>
    /// Converts a neutral planner failure into user-safe text.
    /// </summary>
    /// <param name="failure">The neutral failure metadata.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToUserSafeMessage(PlannerOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return failure.Category switch
        {
            PlannerFailureCategory.Authentication => "Authentication expired. Sign in again and retry.",
            PlannerFailureCategory.Authorisation => "Permission denied. Confirm the required Planner permissions and try again.",
            PlannerFailureCategory.Conflict => "Planner data changed during processing. Run a fresh preview and retry.",
            PlannerFailureCategory.Unavailable => "Planner is temporarily busy. Wait and retry the import.",
            PlannerFailureCategory.Validation => "The request is not valid. Review the input and try again.",
            _ => "An unexpected planner error occurred. Retry and check logs if the issue continues.",
        };
    }
}
