namespace IssueTracker.Web.Models;

/// <summary>A user or system entity that authors or receives posts.</summary>
public class Actor
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name (e.g. "Claude", "Human", "System").</summary>
    public string Name { get; set; } = "";

    /// <summary>Role governing permissions: "Admin", "User", or "AI". Admins can archive any issue.</summary>
    public string Role { get; set; } = "User";
}
