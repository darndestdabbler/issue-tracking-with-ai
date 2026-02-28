using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Projects.OrderBy(p => p.Name).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project project)
    {
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Name))
            project.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(project);
    }

    public class UpdateProjectRequest
    {
        public string Name { get; set; } = "";
    }
}
