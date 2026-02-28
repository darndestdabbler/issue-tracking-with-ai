namespace IssueTracker.Web.Models;

public class Post
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SessionId { get; set; }
    public string? Title { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public int FromActorId { get; set; }
    public int? ToActorId { get; set; }

    /// <summary>New | Discuss | Proceed As Is | Proceed With Mods | Check | Hold | Archive</summary>
    public string ActionType { get; set; } = "";

    /// <summary>FK → Post (self-referencing). NULL = root/new topic.</summary>
    public int? ActionForId { get; set; }

    /// <summary>Open | Closed | Deferred — meaningful only on root posts (ActionForId IS NULL).</summary>
    public string? Status { get; set; }

    /// <summary>Comma-delimited tags, e.g. "auth,security,token".</summary>
    public string? Tags { get; set; }

    public string Text { get; set; } = "";

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public Actor FromActor { get; set; } = null!;
    public Actor? ToActor { get; set; }
    public Post? Parent { get; set; }
    public ICollection<Post> Replies { get; set; } = [];
}
