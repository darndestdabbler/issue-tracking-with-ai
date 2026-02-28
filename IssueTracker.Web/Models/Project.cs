namespace IssueTracker.Web.Models;

/// <summary>Top-level container that groups sessions and posts.</summary>
public class Project
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the project.</summary>
    public string Name { get; set; } = "";
}
