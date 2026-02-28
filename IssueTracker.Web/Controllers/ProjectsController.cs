using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

/// <summary>API controller for project CRUD operations.</summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all projects, ordered by name.</summary>
    /// <returns>All projects.</returns>
    /// <response code="200">Projects returned.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Projects.OrderBy(p => p.Name).ToListAsync());

    /// <summary>Returns a single project by ID.</summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The project.</returns>
    /// <response code="200">Project found.</response>
    /// <response code="404">Project not found.</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound();
        return Ok(project);
    }

    /// <summary>Creates a new project.</summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project.</returns>
    /// <response code="201">Project created.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project project)
    {
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    /// <summary>Renames a project.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="request">The rename payload.</param>
    /// <returns>The updated project.</returns>
    /// <response code="200">Project renamed.</response>
    /// <response code="404">Project not found.</response>
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

    /// <summary>Request body for renaming a project.</summary>
    public class UpdateProjectRequest
    {
        /// <summary>New name for the project.</summary>
        public string Name { get; set; } = "";
    }
}
