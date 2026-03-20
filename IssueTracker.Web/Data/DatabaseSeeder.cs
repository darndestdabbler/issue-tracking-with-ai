using IssueTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Data;

/// <summary>Seeds the database with actors, projects, and demo data on first run.</summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Applies migrations (or EnsureCreated for in-memory SQLite), then seeds
    /// actors, projects, and demo sessions/posts if the tables are empty.
    /// </summary>
    /// <param name="app">The running web application.</param>
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // In-memory SQLite has no migration history table; use EnsureCreated instead
        var connString = db.Database.GetConnectionString();
        if (connString != null && connString.Contains(":memory:"))
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        // Seed Actors
        if (!await db.Actors.AnyAsync())
        {
            db.Actors.AddRange(
                new Actor { Name = "Claude", Role = "AI" },
                new Actor { Name = "Human", Role = "Admin" },
                new Actor { Name = "System", Role = "System" },
                new Actor { Name = "Gemini", Role = "AI" }
            );
            await db.SaveChangesAsync();
        }

        // Seed default Projects
        if (!await db.Projects.AnyAsync())
        {
            db.Projects.AddRange(
                new Models.Project { Name = "Issue Tracker" },
                new Models.Project { Name = "Sample Project" }
            );
            await db.SaveChangesAsync();
        }

        // Seed demo data (only if no sessions exist yet)
        if (!await db.Sessions.AnyAsync())
        {
            await SeedDemoDataAsync(db);
        }
    }

    /// <summary>Creates sample sessions and multi-threaded posts showing the full issue lifecycle.</summary>
    private static async Task SeedDemoDataAsync(AppDbContext db)
    {
        var baseDate = new DateTime(2026, 2, 25, 10, 0, 0, DateTimeKind.Utc);

        // --- Sessions ---
        var session1 = new Session { ProjectId = 1, Name = "Session 001 — Initial Scaffold", StartDate = baseDate, CreatedOn = baseDate };
        var session2 = new Session { ProjectId = 1, Name = "Session 002 — API & Data Model", StartDate = baseDate.AddDays(1), CreatedOn = baseDate.AddDays(1) };
        var session3 = new Session { ProjectId = 1, Name = "Session 003 — UI Implementation", StartDate = baseDate.AddDays(2), CreatedOn = baseDate.AddDays(2) };
        db.Sessions.AddRange(session1, session2, session3);
        await db.SaveChangesAsync();

        // --- Thread 1: Design post-based data model (Closed) ---
        // New → Discuss → Discuss → Archive
        var post1 = new Post
        {
            ProjectId = 1, SessionId = session1.Id, FromActorId = 2, // Human
            ActionType = "New", Title = "Design post-based data model",
            Tags = "architecture,data-model",
            Text = "Need to decide on the core data model. Options: separate Issue/Comment tables vs. a unified Post model where everything is a post with ActionType and parent references.",
            Status = "Open", DateTime = baseDate.AddMinutes(15)
        };
        db.Posts.Add(post1);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session1.Id, FromActorId = 1, // Claude
            ActionType = "Discuss", ActionForId = post1.Id,
            Text = "Recommend the unified Post model. Every action (new issue, discussion, hold, archive) becomes a Post with an ActionType. Parent-child linking via ActionForId. Status lives only on root posts and is updated by child post actions. This gives a complete audit trail with no separate comment or history tables.",
            DateTime = baseDate.AddMinutes(30)
        });

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session1.Id, FromActorId = 2, // Human
            ActionType = "Discuss", ActionForId = post1.Id,
            Text = "Agreed. The unified model keeps the schema simple and makes the API surface smaller. Let's go with Post as the single entity — ActionType drives behavior, ActionForId links replies to parents.",
            DateTime = baseDate.AddMinutes(45)
        });

        var archive1 = new Post
        {
            ProjectId = 1, SessionId = session2.Id, FromActorId = 1, // Claude
            ActionType = "Archive", ActionForId = post1.Id,
            Text = "Data model implemented. Post entity with ActionType, ActionForId, and status propagation is working end-to-end.",
            DateTime = baseDate.AddDays(1).AddMinutes(30)
        };
        db.Posts.Add(archive1);
        post1.Status = "Closed";
        await db.SaveChangesAsync();

        // --- Thread 2: Token refresh returns null on expired sessions (Closed) ---
        // New → Discuss → Check → Archive
        var post2 = new Post
        {
            ProjectId = 1, SessionId = session2.Id, FromActorId = 1, // Claude
            ActionType = "New", Title = "Token refresh returns null on expired sessions",
            Tags = "bug,auth",
            Text = "When a session token expires, the refresh middleware returns null instead of throwing an appropriate error. This causes a NullReferenceException downstream in the authorization pipeline.",
            Status = "Open", DateTime = baseDate.AddDays(1).AddHours(2)
        };
        db.Posts.Add(post2);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session2.Id, FromActorId = 1, // Claude
            ActionType = "Discuss", ActionForId = post2.Id,
            Text = "Root cause: TokenRefreshMiddleware.cs line 47 doesn't check for null expiry date before comparing. Fix: add a null check and return a 401 with a clear error message when the token has no expiry.",
            DateTime = baseDate.AddDays(1).AddHours(2).AddMinutes(30)
        });

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session2.Id, FromActorId = 1, // Claude
            ActionType = "Check", ActionForId = post2.Id, ToActorId = 2, // Requesting Human review
            Text = "Fix applied — added null guard and 401 response. Please review the approach before we close this out.",
            DateTime = baseDate.AddDays(1).AddHours(3)
        });

        var archive2 = new Post
        {
            ProjectId = 1, SessionId = session2.Id, FromActorId = 2, // Human
            ActionType = "Archive", ActionForId = post2.Id,
            Text = "Reviewed and approved. The null guard is clean and the 401 message is clear. Closing.",
            DateTime = baseDate.AddDays(1).AddHours(4)
        };
        db.Posts.Add(archive2);
        post2.Status = "Closed";
        await db.SaveChangesAsync();

        // --- Thread 3: Add pagination to issues grid (Deferred) ---
        // New → Discuss → Hold
        var post3 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 2, // Human
            ActionType = "New", Title = "Add pagination to issues grid",
            Tags = "ui,enhancement",
            Text = "The issues DataGrid currently loads all posts at once. As data grows, we'll need server-side pagination. MudDataGrid supports ServerData for this.",
            Status = "Open", DateTime = baseDate.AddDays(2).AddHours(1)
        };
        db.Posts.Add(post3);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 1, // Claude
            ActionType = "Discuss", ActionForId = post3.Id,
            Text = "MudDataGrid ServerData callback would need a new paginated endpoint. Suggest adding skip/take parameters to GET /api/posts and returning a wrapper with TotalCount. Not urgent while data is small.",
            DateTime = baseDate.AddDays(2).AddHours(1).AddMinutes(30)
        });

        var hold3 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 2, // Human
            ActionType = "Hold", ActionForId = post3.Id,
            Text = "Deferring until data volume justifies the effort. Current dataset is small enough for client-side loading.",
            DateTime = baseDate.AddDays(2).AddHours(2)
        };
        db.Posts.Add(hold3);
        post3.Status = "Deferred";
        await db.SaveChangesAsync();

        // --- Thread 4: Centralize API base URL (Open) ---
        // New → Discuss
        var post4 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 1, // Claude
            ActionType = "New", Title = "Centralize API base URL",
            Tags = "tech-debt,config",
            Text = "The API base URL (http://localhost:5124) is hardcoded in 7+ places across Issues.razor and Sessions.razor. Should be moved to appsettings.json and injected via IConfiguration or a typed HttpClient.",
            Status = "Open", DateTime = baseDate.AddDays(2).AddHours(3)
        };
        db.Posts.Add(post4);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 1, // Claude
            ActionType = "Discuss", ActionForId = post4.Id,
            Text = "Two approaches: (1) Register a named HttpClient with BaseAddress in Program.cs, or (2) add an ApiBaseUrl key to appsettings.json and read via IConfiguration. Option 1 is cleaner for Blazor Server since we're already injecting HttpClient.",
            DateTime = baseDate.AddDays(2).AddHours(3).AddMinutes(20)
        });
        await db.SaveChangesAsync();

        // --- Thread 5: Seed demo data for architecture review (Open) ---
        // New only
        var post5 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 2, // Human
            ActionType = "New", Title = "Seed demo data for architecture review",
            Tags = "demo,onboarding",
            Text = "Need realistic sample data in the database for the architecture director demo. Should show a mix of open, closed, and deferred issues with multi-post threads demonstrating the full workflow.",
            Status = "Open", DateTime = baseDate.AddDays(2).AddHours(4)
        };
        db.Posts.Add(post5);
        await db.SaveChangesAsync();

        // --- Thread 6: Ownership and Resolve workflow demo (Pending Review) ---
        // New (Human) → Discuss (Claude) → Resolve (Claude) — demonstrates Pending Review status
        var post6 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 2, // Human
            ActionType = "New", Title = "Validate error handling in API endpoints",
            Tags = "quality,review",
            Text = "Several API endpoints return generic 500 errors instead of structured Problem Details responses. Need to audit and fix all controllers.",
            Status = "Open", DateTime = baseDate.AddDays(2).AddHours(5)
        };
        db.Posts.Add(post6);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 1, // Claude
            ActionType = "Discuss", ActionForId = post6.Id,
            Text = "Audited all 18 controllers. Found 6 endpoints returning raw exceptions. Applying Problem Details middleware and adding try-catch blocks where needed.",
            DateTime = baseDate.AddDays(2).AddHours(5).AddMinutes(30)
        });

        var resolve6 = new Post
        {
            ProjectId = 1, SessionId = session3.Id, FromActorId = 1, // Claude
            ActionType = "Resolve", ActionForId = post6.Id, ToActorId = 2, // Requesting Human review
            Text = "All 6 endpoints now return Problem Details (RFC 7807). Ready for human review before closing.",
            DateTime = baseDate.AddDays(2).AddHours(6)
        };
        db.Posts.Add(resolve6);
        post6.Status = "Pending Review";
        post6.ToActorId = 2; // Delegate to Human
        await db.SaveChangesAsync();
    }
}
