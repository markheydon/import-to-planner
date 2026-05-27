namespace ImportToPlanner.Web.Components.Pages;

internal sealed record HomeWorkflowStepPresentation(
    int Order,
    string Title,
    HomeWorkflowStepState State,
    string BadgeContent,
    string? Summary,
    string? PrimaryActionLabel);
