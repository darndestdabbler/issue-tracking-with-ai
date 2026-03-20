using IssueTracker.Web.Models;
using IssueTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace IssueTracker.Web.Controllers;

/// <summary>API controller for creating, querying, and updating posts (issues and replies).</summary>
[ApiController]
[Route("api/[controller]")]
public class PostsController(PostService postService) : ControllerBase
{
    /// <summary>Returns posts matching the given filters. Supports optional server-side paging.</summary>
    /// <param name="sessionId">Filter by session.</param>
    /// <param name="projectId">Filter by project.</param>
    /// <param name="status">Filter by status (Open, Closed, Deferred).</param>
    /// <param name="tags">Comma-delimited tags to filter by (AND logic).</param>
    /// <param name="page">Zero-based page index. When present, enables paged response.</param>
    /// <param name="pageSize">Items per page (default 10).</param>
    /// <param name="sortBy">Column to sort by (Title, Status, ActionType, FromActor, DateTime).</param>
    /// <param name="sortDesc">True for descending sort.</param>
    /// <returns>An array of posts, or a paged response with items and totalCount.</returns>
    /// <response code="200">Posts returned successfully.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? sessionId,
        [FromQuery] int? projectId,
        [FromQuery] string? status,
        [FromQuery] string? tags,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false)
    {
        if (page.HasValue)
        {
            var size = pageSize ?? 10;
            var (items, totalCount) = await postService.GetPostsPagedAsync(
                sessionId, projectId, status, tags,
                sortBy, sortDesc, page.Value, size);

            return Ok(new { items = items.Select(Map), totalCount });
        }

        var posts = await postService.GetPostsAsync(sessionId, projectId, status, tags);
        return Ok(posts.Select(Map));
    }

    /// <summary>Returns the most recent baton-tagged root post for a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>The latest baton post.</returns>
    /// <response code="200">Baton found.</response>
    /// <response code="404">No baton posts found for this project.</response>
    [HttpGet("latest-baton")]
    public async Task<IActionResult> GetLatestBaton([FromQuery] int projectId)
    {
        var post = await postService.GetLatestBatonAsync(projectId);
        if (post is null) return NotFound();
        return Ok(Map(post));
    }

    /// <summary>Returns a single post by ID.</summary>
    /// <param name="id">The post ID.</param>
    /// <returns>The post.</returns>
    /// <response code="200">Post found.</response>
    /// <response code="404">Post not found.</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await postService.GetPostAsync(id);
        if (post is null) return NotFound();
        return Ok(Map(post));
    }

    /// <summary>Returns the full thread (root post and all descendants) for a given post.</summary>
    /// <param name="id">The root post ID.</param>
    /// <returns>All posts in the thread, ordered chronologically.</returns>
    /// <response code="200">Thread returned.</response>
    [HttpGet("{id:int}/thread")]
    public async Task<IActionResult> GetThread(int id)
    {
        var thread = await postService.GetThreadAsync(id);
        return Ok(thread.Select(Map));
    }

    /// <summary>Creates a new post (issue or reply). ProjectId is derived from the session.</summary>
    /// <param name="request">The post creation payload.</param>
    /// <returns>The created post.</returns>
    /// <response code="201">Post created.</response>
    /// <response code="400">Invalid session or request.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        try
        {
            var post = new Post
            {
                SessionId = request.SessionId,
                FromActorId = request.FromActorId,
                ToActorId = request.ToActorId,
                ActionType = request.ActionType,
                ActionForId = request.ActionForId,
                Title = request.Title,
                Tags = request.Tags,
                Text = request.Text,
                DateTime = DateTime.UtcNow
            };
            var created = await postService.CreatePostAsync(post);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>Updates the title, tags, and/or text of a root post.</summary>
    /// <param name="id">The post ID.</param>
    /// <param name="request">Fields to update (null fields are left unchanged).</param>
    /// <returns>The updated post.</returns>
    /// <response code="200">Post updated.</response>
    /// <response code="404">Post not found or not a root post.</response>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
    {
        var updated = await postService.UpdatePostAsync(id, request.Title, request.Tags, request.Text);
        if (updated is null) return NotFound();

        // Reload with navigation properties for mapping
        var post = await postService.GetPostAsync(id);
        return Ok(Map(post!));
    }

    /// <summary>Request body for updating a root post.</summary>
    public class UpdatePostRequest
    {
        /// <summary>New title, or null to leave unchanged.</summary>
        public string? Title { get; set; }

        /// <summary>New tags, or null to leave unchanged.</summary>
        public string? Tags { get; set; }

        /// <summary>New text, or null to leave unchanged.</summary>
        public string? Text { get; set; }
    }

    /// <summary>Request body for creating a new post (issue or reply).</summary>
    public class CreatePostRequest
    {
        /// <summary>Session in which this post is created.</summary>
        public int SessionId { get; set; }

        /// <summary>Actor authoring this post.</summary>
        public int FromActorId { get; set; }

        /// <summary>Actor this post is addressed to (optional).</summary>
        public int? ToActorId { get; set; }

        /// <summary>Action type (New, Discuss, Check, Hold, Archive, Reopen, etc.).</summary>
        public string ActionType { get; set; } = "";

        /// <summary>Parent post ID for replies; null for new issues.</summary>
        public int? ActionForId { get; set; }

        /// <summary>Issue title (root posts only).</summary>
        public string? Title { get; set; }

        /// <summary>Comma-delimited tags.</summary>
        public string? Tags { get; set; }

        /// <summary>Body text of the post.</summary>
        public string Text { get; set; } = "";
    }

    private static object Map(Post p) => new
    {
        p.Id,
        p.ProjectId,
        p.SessionId,
        p.Title,
        p.DateTime,
        fromActorId = p.FromActorId,
        fromActor = p.FromActor?.Name,
        toActorId = p.ToActorId,
        toActor = p.ToActor?.Name,
        p.ActionType,
        p.ActionForId,
        p.Status,
        p.Tags,
        p.Text
    };
}
