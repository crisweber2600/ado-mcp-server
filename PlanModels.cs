namespace Ado.Mcp.Models
{
    /// <summary>
    /// Strongly-typed representation of a backlog hierarchy used by the planning tools.
    /// </summary>
    public record Plan(
        string? areaPath,
        string? iterationPath,
        List<Epic> epics
    );

    public record Epic(
        string title,
        string? description,
        List<Feature> features
    );

    public record Feature(
        string title,
        string? description,
        List<Story> stories
    );

    public record Story(
        string title,
        string? description,
        List<string>? acceptanceCriteria,
        List<TaskItem>? tasks
    );

    public record TaskItem(
        string title,
        string? description,
        double? estimateHours
    );
}