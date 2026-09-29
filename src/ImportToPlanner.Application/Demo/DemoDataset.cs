using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Demo;

/// <summary>
/// In-memory synthetic fixtures for demonstration mode.
/// </summary>
public static class DemoDataset
{
    /// <summary>Synthetic Microsoft 365 group container identifier.</summary>
    public const string DemoGroupId = "demo-group-contoso-marketing";

    /// <summary>Synthetic plan identifier.</summary>
    public const string DemoPlanId = "demo-plan-q1-rollout";

    /// <summary>Synthetic bucket identifier.</summary>
    public const string DemoBucketId = "demo-bucket-general";

    /// <summary>Label copy for UI surfaces.</summary>
    public const string DemonstrationLabel = "Demonstration data — not your Microsoft 365 tenant";

    /// <summary>Gets synthetic planner containers.</summary>
    public static IReadOnlyList<PlannerContainer> Containers =>
    [
        new PlannerContainer(DemoGroupId, "Contoso Marketing (demo)", ContainerType.Group),
    ];

    /// <summary>Gets synthetic plans for the demo group.</summary>
    public static IReadOnlyList<PlannerPlan> Plans =>
    [
        new PlannerPlan(DemoPlanId, "Q1 rollout (demo)", DemoGroupId, ContainerType.Group),
    ];

    /// <summary>Gets synthetic buckets for the demo plan.</summary>
    public static IReadOnlyList<PlannerBucket> Buckets =>
    [
        new PlannerBucket(DemoBucketId, "General", DemoPlanId),
    ];

    /// <summary>Gets synthetic task snapshots for idempotency preview.</summary>
    public static IReadOnlyList<PlannerTaskSnapshot> ExistingTasks => [];

    /// <summary>Gets synthetic plan members.</summary>
    public static IReadOnlyList<PlanMember> PlanMembers =>
    [
        new PlanMember("demo-user-alex", "alex.example@contoso.com", "alex.example@contoso.com"),
        new PlanMember("demo-user-beth", "beth.example@contoso.com", "beth.example@contoso.com"),
    ];

    /// <summary>Gets a synthetic CSV parse result for uploads during demo mode.</summary>
    public static CsvParseResult SampleCsvParseResult =>
        new(
            [
                new CsvTaskRow(2, "Prepare demo board", "Synthetic task for screenshots", 5, "General", "Launch"),
                new CsvTaskRow(3, "Review import preview", "Walk through validation and preview", 3, "General", null),
            ],
            []);

    /// <summary>Gets synthetic CSV header labels.</summary>
    public static CsvHeaderPeekResult SampleHeaderPeek =>
        new()
        {
            Headers = ["Task Name", "Description", "Priority", "Bucket", "Goal"],
            ValidationErrors = [],
        };
}
