namespace IssueTracker.Web.Components;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class SessionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public int PostCount { get; set; }
}

public class ActorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class PostDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SessionId { get; set; }
    public string? Title { get; set; }
    public DateTime DateTime { get; set; }
    public int FromActorId { get; set; }
    public string? FromActor { get; set; }
    public int? ToActorId { get; set; }
    public string? ToActor { get; set; }
    public string ActionType { get; set; } = "";
    public int? ActionForId { get; set; }
    public string? Status { get; set; }
    public string? Tags { get; set; }
    public string Text { get; set; } = "";
}

public class CreateIssueRequest
{
    public int SessionId { get; set; }
    public int FromActorId { get; set; }
    public string ActionType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string? Tags { get; set; }
    public int? ToActorId { get; set; }
}

public class ReplyRequest
{
    public int ActionForId { get; set; }
    public int SessionId { get; set; }
    public int FromActorId { get; set; }
    public string ActionType { get; set; } = "Discuss";
    public string Text { get; set; } = "";
    public int? ToActorId { get; set; }
}

public class UpdatePostRequest
{
    public string? Title { get; set; }
    public string? Tags { get; set; }
    public string? Text { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
