using IssueTracker.Web.Models;
using IssueTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace IssueTracker.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController(PostService postService) : ControllerBase
{
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await postService.GetPostAsync(id);
        if (post is null) return NotFound();
        return Ok(Map(post));
    }

    [HttpGet("{id:int}/thread")]
    public async Task<IActionResult> GetThread(int id)
    {
        var thread = await postService.GetThreadAsync(id);
        return Ok(thread.Select(Map));
    }

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
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
    {
        var updated = await postService.UpdatePostAsync(id, request.Title, request.Tags, request.Text);
        if (updated is null) return NotFound();

        // Reload with navigation properties for mapping
        var post = await postService.GetPostAsync(id);
        return Ok(Map(post!));
    }

    public class UpdatePostRequest
    {
        public string? Title { get; set; }
        public string? Tags { get; set; }
        public string? Text { get; set; }
    }

    public class CreatePostRequest
    {
        public int SessionId { get; set; }
        public int FromActorId { get; set; }
        public int? ToActorId { get; set; }
        public string ActionType { get; set; } = "";
        public int? ActionForId { get; set; }
        public string? Title { get; set; }
        public string? Tags { get; set; }
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
