using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Services;

/// <summary>Business logic for creating, querying, and updating posts (issues and replies).</summary>
public class PostService(AppDbContext db)
{
    /// <summary>
    /// Creates a post, deriving ProjectId from its session and propagating status
    /// changes (Hold→Deferred, Archive→Closed, Resolve→Pending Review, Reopen→Open) to the root post.
    /// Archive is restricted to the issue owner, a delegated reviewer (root ToActorId), or an Admin.
    /// </summary>
    /// <param name="post">The post to create. SessionId must reference an existing session.</param>
    /// <returns>The created post with its generated Id.</returns>
    /// <exception cref="ArgumentException">Thrown when the referenced session does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a non-owner/non-Admin attempts to archive.</exception>
    public async Task<Post> CreatePostAsync(Post post)
    {
        // Derive ProjectId from Session
        var session = await db.Sessions.FindAsync(post.SessionId)
            ?? throw new ArgumentException($"Session {post.SessionId} not found.");
        post.ProjectId = session.ProjectId;

        // Status rules for root posts
        if (post.ActionForId is null)
        {
            post.Status = post.ActionType == "New" ? "Open" : post.Status;
        }
        else
        {
            // Update root post status based on child action type
            var root = await FindRootAsync(post.ActionForId.Value);
            if (root is not null)
            {
                // Archive enforcement: only owner, delegate, or Admin can archive
                if (post.ActionType == "Archive")
                {
                    var actor = await db.Actors.FindAsync(post.FromActorId);
                    if (actor?.Role != "Admin"
                        && post.FromActorId != root.FromActorId
                        && post.FromActorId != root.ToActorId)
                    {
                        throw new InvalidOperationException(
                            "Only the issue owner, a delegated reviewer, or an Admin can archive.");
                    }
                }

                root.Status = post.ActionType switch
                {
                    "Hold"    => "Deferred",
                    "Archive" => "Closed",
                    "Resolve" => "Pending Review",
                    "Reopen"  => "Open",
                    _         => root.Status
                };

                // Propagate ToActorId to root post to track current assignee
                if (post.ToActorId.HasValue)
                {
                    root.ToActorId = post.ToActorId;
                }
            }
        }

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        // Auto-archive: when a new baton-tagged root post is created,
        // close all previously-Open baton posts for the same project.
        if (post.ActionForId is null && post.ActionType == "New" && HasTag(post.Tags, "baton"))
        {
            var openBatons = await db.Posts
                .Where(p => p.ProjectId == post.ProjectId
                    && p.ActionForId == null
                    && p.Status == "Open"
                    && p.Id != post.Id
                    && p.Tags != null && EF.Functions.Like(p.Tags, "%baton%"))
                .ToListAsync();

            foreach (var oldBaton in openBatons)
            {
                oldBaton.Status = "Closed";
                db.Posts.Add(new Post
                {
                    ProjectId = oldBaton.ProjectId,
                    SessionId = post.SessionId,
                    FromActorId = 3, // System actor
                    ActionType = "Archive",
                    ActionForId = oldBaton.Id,
                    Text = $"Auto-archived: superseded by baton post #{post.Id}.",
                    DateTime = DateTime.UtcNow
                });
            }

            if (openBatons.Count > 0)
                await db.SaveChangesAsync();
        }

        return post;
    }

    /// <summary>Returns root posts matching the given filters, ordered by date descending.</summary>
    /// <param name="sessionId">Optional session filter.</param>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="status">Optional status filter (Open, Closed, Deferred).</param>
    /// <param name="tags">Optional comma-delimited tags; all must match (AND logic).</param>
    /// <returns>Filtered list of root posts with FromActor and ToActor included.</returns>
    public async Task<List<Post>> GetPostsAsync(int? sessionId, int? projectId, string? status, string? tags)
    {
        var query = db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .AsQueryable();

        if (sessionId.HasValue)
            query = query.Where(p => p.SessionId == sessionId.Value);

        if (projectId.HasValue)
            query = query.Where(p => p.ProjectId == projectId.Value);

        // Always filter to root posts (issues) — replies are shown via thread expansion
        query = query.Where(p => p.ActionForId == null);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(tags))
        {
            foreach (var tag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var t = tag;
                query = query.Where(p => p.Tags != null && EF.Functions.Like(p.Tags, $"%{t}%"));
            }
        }

        return await query.OrderByDescending(p => p.DateTime).ToListAsync();
    }

    /// <summary>Returns a page of root posts with total count for server-side paging.</summary>
    /// <param name="sessionId">Optional session filter.</param>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="tags">Optional comma-delimited tags (AND logic).</param>
    /// <param name="sortBy">Column name to sort by (Title, Status, ActionType, FromActor, DateTime).</param>
    /// <param name="sortDescending">True for descending sort order.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A tuple of the page items and the total matching count.</returns>
    public async Task<(List<Post> Items, int TotalCount)> GetPostsPagedAsync(
        int? sessionId, int? projectId, string? status, string? tags,
        string? sortBy, bool sortDescending, int page, int pageSize)
    {
        var query = db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .AsQueryable();

        if (sessionId.HasValue)
            query = query.Where(p => p.SessionId == sessionId.Value);

        if (projectId.HasValue)
            query = query.Where(p => p.ProjectId == projectId.Value);

        // Always filter to root posts (issues) — replies are shown via thread expansion
        query = query.Where(p => p.ActionForId == null);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(tags))
        {
            foreach (var tag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var t = tag;
                query = query.Where(p => p.Tags != null && EF.Functions.Like(p.Tags, $"%{t}%"));
            }
        }

        var totalCount = await query.CountAsync();

        query = sortBy switch
        {
            "Title"      => sortDescending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
            "Status"     => sortDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "ActionType" => sortDescending ? query.OrderByDescending(p => p.ActionType) : query.OrderBy(p => p.ActionType),
            "FromActor"  => sortDescending ? query.OrderByDescending(p => p.FromActor!.Name) : query.OrderBy(p => p.FromActor!.Name),
            "DateTime"   => sortDescending ? query.OrderByDescending(p => p.DateTime) : query.OrderBy(p => p.DateTime),
            _            => query.OrderByDescending(p => p.DateTime),
        };

        var items = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>Returns a single post by ID with actor navigation properties, or null if not found.</summary>
    /// <param name="id">The post ID.</param>
    /// <returns>The post, or null.</returns>
    public async Task<Post?> GetPostAsync(int id)
        => await db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>
    /// Returns the full thread (root + all descendants) using a recursive CTE.
    /// Compatible with both SQLite and SQL Server.
    /// </summary>
    /// <param name="rootId">The root post ID.</param>
    /// <returns>All posts in the thread, ordered by date ascending.</returns>
    public async Task<List<Post>> GetThreadAsync(int rootId)
    {
        // Recursive CTE fetches the entire thread tree in a single query.
        // SQLite requires WITH RECURSIVE; SQL Server uses WITH.
        var isSqlite = db.Database.ProviderName?.Contains("Sqlite",
            StringComparison.OrdinalIgnoreCase) == true;
        var cteKeyword = isSqlite ? "WITH RECURSIVE" : "WITH";

        var sql = cteKeyword + """
             ThreadCte AS (
                SELECT "Id" FROM "Posts" WHERE "Id" = {0}
                UNION ALL
                SELECT p."Id" FROM "Posts" p
                INNER JOIN ThreadCte t ON p."ActionForId" = t."Id"
            )
            SELECT p.* FROM "Posts" p
            INNER JOIN ThreadCte t ON p."Id" = t."Id"
            """;

        return await db.Posts
            .FromSqlRaw(sql, rootId)
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .OrderBy(p => p.DateTime)
            .ToListAsync();
    }

    /// <summary>Updates the title, tags, and/or text of a root post. Non-root posts cannot be edited.</summary>
    /// <param name="id">The post ID.</param>
    /// <param name="title">New title, or null to leave unchanged.</param>
    /// <param name="tags">New tags, or null to leave unchanged.</param>
    /// <param name="text">New text, or null to leave unchanged.</param>
    /// <returns>The updated post, or null if not found or not a root post.</returns>
    public async Task<Post?> UpdatePostAsync(int id, string? title, string? tags, string? text)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return null;
        if (post.ActionForId is not null) return null; // Only root posts can be edited

        if (title is not null) post.Title = title;
        if (tags is not null) post.Tags = tags;
        if (text is not null) post.Text = text;

        await db.SaveChangesAsync();
        return post;
    }

    /// <summary>Returns the most recent baton-tagged root post for a project, or null if none exist.</summary>
    /// <param name="projectId">The project to search in.</param>
    /// <returns>The latest baton post with actor navigation properties, or null.</returns>
    public async Task<Post?> GetLatestBatonAsync(int projectId)
    {
        return await db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .Where(p => p.ProjectId == projectId
                && p.ActionForId == null
                && p.Tags != null && EF.Functions.Like(p.Tags, "%baton%"))
            .OrderByDescending(p => p.DateTime)
            .FirstOrDefaultAsync();
    }

    /// <summary>Returns true if the comma-delimited tags string contains the given tag (case-insensitive).</summary>
    private static bool HasTag(string? tags, string tag)
        => tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>Walks up the ActionForId chain to find the root post of a thread.</summary>
    private async Task<Post?> FindRootAsync(int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null) return null;
        if (post.ActionForId is null) return post;
        return await FindRootAsync(post.ActionForId.Value);
    }
}
