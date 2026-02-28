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
    public async Task<IActionResult> GetAll([FromQuery] int? projectId)
    {
        var query = db.Sessions.Include(s => s.Project).AsQueryable();
        if (projectId.HasValue)
            query = query.Where(s => s.ProjectId == projectId.Value);
        var sessions = await query.OrderByDescending(s => s.StartDate).ToListAsync();
        return Ok(sessions.Select(s => new { s.Id, s.Name, s.ProjectId, projectName = s.Project.Name, s.StartDate, s.CreatedOn }));
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

    public class CreateSessionRequest
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
    }
}
