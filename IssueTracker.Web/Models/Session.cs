namespace IssueTracker.Web.Models;

/// <summary>A work session within a project. Posts inherit their ProjectId from their session.</summary>
public class Session
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Models.Project"/>.</summary>
    public int ProjectId { get; set; }

    /// <summary>Display name of the session.</summary>
    public string Name { get; set; } = "";

    /// <summary>When the session started (UTC).</summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>Record creation timestamp (UTC).</summary>
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    /// <summary>Soft-delete flag. Archived sessions are hidden by default.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Navigation to the owning project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Posts created during this session.</summary>
    public ICollection<Post> Posts { get; set; } = [];
}
