using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Services;

public class PostService(AppDbContext db)
{
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
                root.Status = post.ActionType switch
                {
                    "Hold"    => "Deferred",
                    "Archive" => "Closed",
                    _         => root.Status
                };
            }
        }

        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return post;
    }

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

        if (!string.IsNullOrEmpty(status))
        {
            // Status only meaningful on root posts
            query = query.Where(p => p.ActionForId == null && p.Status == status);
        }

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

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.ActionForId == null && p.Status == status);

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

    public async Task<Post?> GetPostAsync(int id)
        => await db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Post>> GetThreadAsync(int rootId)
    {
        var all = new List<Post>();
        await CollectThreadAsync(rootId, all);
        return all.OrderBy(p => p.DateTime).ToList();
    }

    private async Task CollectThreadAsync(int postId, List<Post> collected)
    {
        var post = await db.Posts
            .Include(p => p.FromActor)
            .Include(p => p.ToActor)
            .Include(p => p.Replies)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null) return;

        collected.Add(post);
        foreach (var reply in post.Replies)
            await CollectThreadAsync(reply.Id, collected);
    }

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

    private async Task<Post?> FindRootAsync(int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null) return null;
        if (post.ActionForId is null) return post;
        return await FindRootAsync(post.ActionForId.Value);
    }
}
