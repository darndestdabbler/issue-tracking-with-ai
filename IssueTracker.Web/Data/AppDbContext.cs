using IssueTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Data;

/// <summary>EF Core database context for the issue tracker.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Projects table.</summary>
    public DbSet<Models.Project> Projects => Set<Models.Project>();

    /// <summary>Sessions table.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>Actors table.</summary>
    public DbSet<Actor> Actors => Set<Actor>();

    /// <summary>Posts table.</summary>
    public DbSet<Post> Posts => Set<Post>();

    /// <summary>Configures self-referencing Post relationships and restricts cascade deletes on Actor FKs.</summary>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Self-referencing: Post → Parent (via ActionForId)
        mb.Entity<Post>()
            .HasOne(p => p.Parent)
            .WithMany(p => p.Replies)
            .HasForeignKey(p => p.ActionForId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post → FromActor (must restrict to avoid multiple cascade paths)
        mb.Entity<Post>()
            .HasOne(p => p.FromActor)
            .WithMany()
            .HasForeignKey(p => p.FromActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post → ToActor (nullable, restrict)
        mb.Entity<Post>()
            .HasOne(p => p.ToActor)
            .WithMany()
            .HasForeignKey(p => p.ToActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
