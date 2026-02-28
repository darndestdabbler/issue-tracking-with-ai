using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Actors.OrderBy(a => a.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Actor actor)
    {
        db.Actors.Add(actor);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = actor.Id }, actor);
    }
}
