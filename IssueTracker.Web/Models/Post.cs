namespace IssueTracker.Web.Models;

/// <summary>
/// Universal entity representing issues, replies, and state-change actions.
/// A root post (ActionForId is null) is an issue; child posts are discussion or status updates.
/// </summary>
public class Post
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Project"/>. Derived from the session at creation time.</summary>
    public int ProjectId { get; set; }

    /// <summary>FK → <see cref="Models.Session"/> in which this post was created.</summary>
    public int SessionId { get; set; }

    /// <summary>Issue title. Present on root posts; null on replies.</summary>
    public string? Title { get; set; }

    /// <summary>Timestamp (UTC) when the post was created.</summary>
    public DateTime DateTime { get; set; } = DateTime.UtcNow;

    /// <summary>FK → <see cref="Actor"/> who authored this post.</summary>
    public int FromActorId { get; set; }

    /// <summary>FK → <see cref="Actor"/> this post is addressed to (optional).</summary>
    public int? ToActorId { get; set; }

    /// <summary>New | Discuss | Proceed As Is | Proceed With Mods | Check | Hold | Archive | Reopen</summary>
    public string ActionType { get; set; } = "";

    /// <summary>FK → Post (self-referencing). NULL = root/new topic.</summary>
    public int? ActionForId { get; set; }

    /// <summary>Open | Closed | Deferred — meaningful only on root posts (ActionForId IS NULL).</summary>
    public string? Status { get; set; }

    /// <summary>Comma-delimited tags, e.g. "auth,security,token".</summary>
    public string? Tags { get; set; }

    /// <summary>Body text of the post.</summary>
    public string Text { get; set; } = "";

    // Navigation properties

    /// <summary>Navigation to the owning project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Navigation to the session.</summary>
    public Session Session { get; set; } = null!;

    /// <summary>Navigation to the authoring actor.</summary>
    public Actor FromActor { get; set; } = null!;

    /// <summary>Navigation to the target actor (optional).</summary>
    public Actor? ToActor { get; set; }

    /// <summary>Navigation to the parent post (null for root posts).</summary>
    public Post? Parent { get; set; }

    /// <summary>Child posts (replies and status-change actions).</summary>
    public ICollection<Post> Replies { get; set; } = [];
}
