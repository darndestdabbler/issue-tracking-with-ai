using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(AppDbContext db) : ControllerBase
{
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await db.Sessions.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == id);
        if (s is null) return NotFound();
        return Ok(new { s.Id, s.Name, s.ProjectId, projectName = s.Project.Name, s.StartDate, s.CreatedOn });
    }

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

    [HttpPut("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var session = await db.Sessions.FindAsync(id);
        if (session is null) return NotFound();
        session.IsArchived = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var session = await db.Sessions.FindAsync(id);
        if (session is null) return NotFound();
        session.IsArchived = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    public class CreateSessionRequest
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
    }

    public class UpdateSessionRequest
    {
        public string Name { get; set; } = "";
    }
}
