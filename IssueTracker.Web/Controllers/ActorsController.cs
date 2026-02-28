using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

/// <summary>API controller for actor reference data (Claude, Human, System).</summary>
[ApiController]
[Route("api/[controller]")]
public class ActorsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all actors, ordered by ID.</summary>
    /// <returns>All actors.</returns>
    /// <response code="200">Actors returned.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Actors.OrderBy(a => a.Id).ToListAsync());

    /// <summary>Creates a new actor.</summary>
    /// <param name="actor">The actor to create.</param>
    /// <returns>The created actor.</returns>
    /// <response code="201">Actor created.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Actor actor)
    {
        db.Actors.Add(actor);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = actor.Id }, actor);
    }
}
