namespace IssueTracker.Web.Models;

public class Session
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
}
