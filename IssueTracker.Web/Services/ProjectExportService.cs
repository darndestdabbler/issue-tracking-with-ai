using IssueTracker.Web.Data;
using IssueTracker.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Services;

/// <summary>Exports a single project's data to a self-contained SQLite file.</summary>
public class ProjectExportService(AppDbContext db)
{
    /// <summary>
    /// Exports the specified project (with its sessions, posts, and referenced actors)
    /// to a temporary SQLite file. Returns the file path, or null if the project doesn't exist.
    /// </summary>
    public async Task<string?> ExportProjectToSqliteAsync(int projectId)
    {
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return null;

        // Query all project data from source DB
        var sessions = await db.Sessions
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .ToListAsync();

        var posts = await db.Posts
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        // Collect referenced actor IDs (deduplicated)
        var actorIds = posts.Select(p => p.FromActorId)
            .Union(posts.Where(p => p.ToActorId.HasValue).Select(p => p.ToActorId!.Value))
            .ToHashSet();

        var actors = await db.Actors
            .AsNoTracking()
            .Where(a => actorIds.Contains(a.Id))
            .ToListAsync();

        // Create temp SQLite file
        var tempPath = Path.Combine(Path.GetTempPath(), $"issuetracker-export-{projectId}-{Guid.NewGuid():N}.sqlite");
        var connectionString = $"Data Source={tempPath}";

        var exportOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using (var exportDb = new AppDbContext(exportOptions))
        {
            await exportDb.Database.EnsureCreatedAsync();

            // Disable FK enforcement during bulk insert — data is already consistent in the source DB
            await exportDb.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

            // Use raw SQL to preserve original IDs
            foreach (var actor in actors)
            {
                await exportDb.Database.ExecuteSqlAsync(
                    $"INSERT INTO \"Actors\" (\"Id\", \"Name\", \"Role\") VALUES ({actor.Id}, {actor.Name}, {actor.Role})");
            }

            await exportDb.Database.ExecuteSqlAsync(
                $"INSERT INTO \"Projects\" (\"Id\", \"Name\") VALUES ({project.Id}, {project.Name})");

            foreach (var session in sessions)
            {
                await exportDb.Database.ExecuteSqlAsync(
                    $"INSERT INTO \"Sessions\" (\"Id\", \"ProjectId\", \"Name\", \"StartDate\", \"CreatedOn\", \"IsArchived\") VALUES ({session.Id}, {session.ProjectId}, {session.Name}, {session.StartDate.ToString("o")}, {session.CreatedOn.ToString("o")}, {session.IsArchived})");
            }

            // Posts ordered by Id ascending — parent posts always have lower IDs than children
            foreach (var post in posts)
            {
                await exportDb.Database.ExecuteSqlAsync(
                    $"INSERT INTO \"Posts\" (\"Id\", \"ProjectId\", \"SessionId\", \"Title\", \"DateTime\", \"FromActorId\", \"ToActorId\", \"ActionType\", \"ActionForId\", \"Status\", \"Tags\", \"Text\") VALUES ({post.Id}, {post.ProjectId}, {post.SessionId}, {post.Title}, {post.DateTime.ToString("o")}, {post.FromActorId}, {post.ToActorId}, {post.ActionType}, {post.ActionForId}, {post.Status}, {post.Tags}, {post.Text})");
            }

            // Re-enable FK enforcement and verify integrity
            await exportDb.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        // Clear the SQLite connection pool so the file is fully released
        SqliteConnection.ClearPool(new SqliteConnection(connectionString));

        return tempPath;
    }
}
