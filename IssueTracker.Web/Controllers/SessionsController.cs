using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

/// <summary>API controller for session lifecycle: create, list, rename, archive, and restore.</summary>
[ApiController]
[Route("api/[controller]")]
public class SessionsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists sessions, optionally filtered by project. Excludes archived sessions by default.</summary>
    /// <param name="projectId">Filter by project.</param>
    /// <param name="includeArchived">When true, includes archived sessions in the results.</param>
    /// <returns>Sessions with post counts, ordered by start date descending.</returns>
    /// <response code="200">Sessions returned.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? projectId, [FromQuery] bool includeArchived = false)
    {
        var query = db.Sessions.Include(s => s.Project).AsQueryable();
        if (projectId.HasValue)
            query = query.Where(s => s.ProjectId == projectId.Value);
        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);
        var sessions = await query
            .OrderByDescending(s => s.StartDate)
            .Select(s => new { s.Id, s.Name, s.ProjectId, projectName = s.Project.Name, s.StartDate, s.CreatedOn, s.IsArchived, postCount = s.Posts.Count })
            .ToListAsync();
        return Ok(sessions);
    }

    /// <summary>Returns a single session by ID.</summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The session.</returns>
    /// <response code="200">Session found.</response>
    /// <response code="404">Session not found.</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await db.Sessions.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == id);
        if (s is null) return NotFound();
        return Ok(new { s.Id, s.Name, s.ProjectId, projectName = s.Project.Name, s.StartDate, s.CreatedOn });
    }

    /// <summary>Creates a new session in the specified project.</summary>
    /// <param name="request">The session creation payload.</param>
    /// <returns>The created session.</returns>
    /// <response code="201">Session created.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
    {
        var session = new Session
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            StartDate = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = session.Id },
            new { session.Id, session.Name, session.ProjectId, session.StartDate });
    }

    /// <summary>Renames a session.</summary>
    /// <param name="id">The session ID.</param>
    /// <param name="request">The rename payload.</param>
    /// <returns>The updated session.</returns>
    /// <response code="200">Session renamed.</response>
    /// <response code="404">Session not found.</response>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSessionRequest request)
    {
        var session = await db.Sessions.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Name))
            session.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(new { session.Id, session.Name, session.ProjectId, projectName = session.Project.Name, session.StartDate, session.CreatedOn, session.IsArchived });
    }

    /// <summary>Archives (soft-deletes) a session.</summary>
    /// <param name="id">The session ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Session archived.</response>
    /// <response code="404">Session not found.</response>
    [HttpPut("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var session = await db.Sessions.FindAsync(id);
        if (session is null) return NotFound();
        session.IsArchived = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Restores an archived session.</summary>
    /// <param name="id">The session ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Session restored.</response>
    /// <response code="404">Session not found.</response>
    [HttpPut("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var session = await db.Sessions.FindAsync(id);
        if (session is null) return NotFound();
        session.IsArchived = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Request body for creating a session.</summary>
    public class CreateSessionRequest
    {
        /// <summary>Project to create the session in.</summary>
        public int ProjectId { get; set; }

        /// <summary>Display name for the session.</summary>
        public string Name { get; set; } = "";
    }

    /// <summary>Request body for renaming a session.</summary>
    public class UpdateSessionRequest
    {
        /// <summary>New name for the session.</summary>
        public string Name { get; set; } = "";
    }
}
