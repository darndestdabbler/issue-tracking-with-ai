namespace IssueTracker.Web.Components;

/// <summary>Client-side DTO for projects.</summary>
public class ProjectDto
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";
}

/// <summary>Client-side DTO for sessions, including computed post count.</summary>
public class SessionDto
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>FK to the owning project.</summary>
    public int ProjectId { get; set; }

    /// <summary>Resolved project name.</summary>
    public string? ProjectName { get; set; }

    /// <summary>When the session started (UTC).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Record creation timestamp (UTC).</summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>Number of posts in this session (server-computed).</summary>
    public int PostCount { get; set; }

    /// <summary>Whether this session has been archived.</summary>
    public bool IsArchived { get; set; }
}

/// <summary>Client-side DTO for actors.</summary>
public class ActorDto
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Role governing permissions: "Admin", "User", or "AI".</summary>
    public string Role { get; set; } = "User";
}

/// <summary>Client-side DTO for posts (issues and replies).</summary>
public class PostDto
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>FK to the project.</summary>
    public int ProjectId { get; set; }

    /// <summary>FK to the session.</summary>
    public int SessionId { get; set; }

    /// <summary>Issue title (present on root posts; null on replies).</summary>
    public string? Title { get; set; }

    /// <summary>Post creation timestamp (UTC).</summary>
    public DateTime DateTime { get; set; }

    /// <summary>FK to the authoring actor.</summary>
    public int FromActorId { get; set; }

    /// <summary>Resolved name of the authoring actor.</summary>
    public string? FromActor { get; set; }

    /// <summary>FK to the target actor (optional).</summary>
    public int? ToActorId { get; set; }

    /// <summary>Resolved name of the target actor.</summary>
    public string? ToActor { get; set; }

    /// <summary>Action type (New, Discuss, Check, Hold, Archive, Reopen, etc.).</summary>
    public string ActionType { get; set; } = "";

    /// <summary>Parent post ID for replies; null for root issues.</summary>
    public int? ActionForId { get; set; }

    /// <summary>Open | Closed | Deferred — only meaningful on root posts.</summary>
    public string? Status { get; set; }

    /// <summary>Comma-delimited tags.</summary>
    public string? Tags { get; set; }

    /// <summary>Body text of the post.</summary>
    public string Text { get; set; } = "";
}

/// <summary>Request DTO for creating a new issue (root post) from the UI.</summary>
public class CreateIssueRequest
{
    /// <summary>Session to create the issue in.</summary>
    public int SessionId { get; set; }

    /// <summary>Actor authoring this issue.</summary>
    public int FromActorId { get; set; }

    /// <summary>Action type (typically "New").</summary>
    public string ActionType { get; set; } = "";

    /// <summary>Issue title.</summary>
    public string Title { get; set; } = "";

    /// <summary>Issue body text.</summary>
    public string Text { get; set; } = "";

    /// <summary>Comma-delimited tags (optional).</summary>
    public string? Tags { get; set; }

    /// <summary>Actor this issue is addressed to (optional).</summary>
    public int? ToActorId { get; set; }
}

/// <summary>Request DTO for replying to an existing post from the UI.</summary>
public class ReplyRequest
{
    /// <summary>Parent post ID to reply to.</summary>
    public int ActionForId { get; set; }

    /// <summary>Session in which the reply is created.</summary>
    public int SessionId { get; set; }

    /// <summary>Actor authoring this reply.</summary>
    public int FromActorId { get; set; }

    /// <summary>Action type (Discuss, Check, Hold, Archive, Reopen, etc.).</summary>
    public string ActionType { get; set; } = "Discuss";

    /// <summary>Reply body text.</summary>
    public string Text { get; set; } = "";

    /// <summary>Actor this reply is addressed to (optional).</summary>
    public int? ToActorId { get; set; }
}

/// <summary>Request DTO for editing a root post from the UI.</summary>
public class UpdatePostRequest
{
    /// <summary>New title, or null to leave unchanged.</summary>
    public string? Title { get; set; }

    /// <summary>New tags, or null to leave unchanged.</summary>
    public string? Tags { get; set; }

    /// <summary>New text, or null to leave unchanged.</summary>
    public string? Text { get; set; }
}

/// <summary>Generic wrapper for paged API responses.</summary>
/// <typeparam name="T">The item type.</typeparam>
public class PagedResponse<T>
{
    /// <summary>Page of items.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Total number of matching items across all pages.</summary>
    public int TotalCount { get; set; }
}
