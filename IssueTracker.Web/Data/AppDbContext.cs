using IssueTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Models.Project> Projects => Set<Models.Project>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Post> Posts => Set<Post>();

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
