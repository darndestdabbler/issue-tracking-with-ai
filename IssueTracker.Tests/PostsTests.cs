using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IssueTracker.Tests;

public class PostsTests : IClassFixture<IssueTrackerFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PostsTests(IssueTrackerFactory factory) => _client = factory.CreateClient();

    // ---- GET /api/posts (list) ----

    [Fact]
    public async Task GetAll_ReturnsRootPostsOnly()
    {
        var posts = await _client.GetFromJsonAsync<List<PostDto>>("/api/posts?projectId=1", JsonOptions);

        Assert.NotNull(posts);
        Assert.True(posts.Count > 0);
        // All returned posts should be root posts (no ActionForId)
        Assert.All(posts, p => Assert.Null(p.ActionForId));
    }

    [Fact]
    public async Task GetAll_FilterByStatus()
    {
        var closedPosts = await _client.GetFromJsonAsync<List<PostDto>>(
            "/api/posts?status=Closed&projectId=1", JsonOptions);

        Assert.NotNull(closedPosts);
        Assert.True(closedPosts.Count > 0);
        Assert.All(closedPosts, p => Assert.Equal("Closed", p.Status));
    }

    [Fact]
    public async Task GetAll_FilterByTags()
    {
        // Seed data has a post tagged "architecture,data-model"
        var posts = await _client.GetFromJsonAsync<List<PostDto>>(
            "/api/posts?tags=architecture&projectId=1", JsonOptions);

        Assert.NotNull(posts);
        Assert.True(posts.Count > 0);
        Assert.All(posts, p => Assert.Contains("architecture", p.Tags!));
    }

    [Fact]
    public async Task GetAll_Paged_ReturnsItemsAndTotalCount()
    {
        var response = await _client.GetAsync("/api/posts?projectId=1&page=0&pageSize=2");
        response.EnsureSuccessStatusCode();

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse>(JsonOptions);
        Assert.NotNull(paged);
        Assert.True(paged.TotalCount > 0);
        Assert.True(paged.Items.Count <= 2);
    }

    // ---- GET /api/posts/{id} ----

    [Fact]
    public async Task GetById_ReturnsPost()
    {
        var post = await _client.GetFromJsonAsync<PostDto>("/api/posts/1", JsonOptions);

        Assert.NotNull(post);
        Assert.Equal(1, post.Id);
        Assert.NotNull(post.FromActor);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/posts/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- GET /api/posts/{id}/thread ----

    [Fact]
    public async Task GetThread_ReturnsFullTree()
    {
        // Post 1 (seed data) is a root post with replies
        var thread = await _client.GetFromJsonAsync<List<PostDto>>("/api/posts/1/thread", JsonOptions);

        Assert.NotNull(thread);
        Assert.True(thread.Count > 1, "Thread should include root + replies");
        // First item should be the root post
        Assert.Equal(1, thread[0].Id);
    }

    // ---- POST /api/posts (create) ----

    [Fact]
    public async Task Create_RootPost_StatusOpen()
    {
        var response = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1,
            fromActorId = 1,
            actionType = "New",
            title = "Test Issue",
            tags = "test",
            text = "A test issue."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PostDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Open", created.Status);
        Assert.Equal("New", created.ActionType);
        Assert.Null(created.ActionForId);
        Assert.Equal(1, created.ProjectId); // derived from session
    }

    [Fact]
    public async Task Create_Reply_HoldDeferrsRoot()
    {
        // Create a root post
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Hold Test", text = "Will be deferred."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Hold it
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Hold",
            actionForId = root!.Id, text = "Deferring."
        });

        // Verify root status changed
        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Deferred", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_ArchiveClosesRoot()
    {
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Archive Test", text = "Will be closed."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Archive",
            actionForId = root!.Id, text = "Closing."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Closed", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_ReopensRoot()
    {
        // Create and close a root post
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Reopen Test", text = "Will be reopened."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Archive",
            actionForId = root!.Id, text = "Closing first."
        });

        // Reopen it
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Reopen",
            actionForId = root.Id, text = "Reopening."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task Create_InvalidSession_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 999, fromActorId = 1, actionType = "New",
            title = "Bad Session", text = "Should fail."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- PUT /api/posts/{id} (update) ----

    [Fact]
    public async Task Update_RootPost()
    {
        // Create a root post to update
        var createResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Original Title", tags = "original", text = "Original text."
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var updateResponse = await _client.PutAsJsonAsync($"/api/posts/{created!.Id}", new
        {
            title = "Updated Title", tags = "updated", text = "Updated text."
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);
        Assert.Equal("Updated Title", updated!.Title);
        Assert.Equal("updated", updated.Tags);
        Assert.Equal("Updated text.", updated.Text);
    }

    [Fact]
    public async Task Update_Reply_Returns404()
    {
        // Create root + reply
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Update Reply Test", text = "Root post."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var replyResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Discuss",
            actionForId = root!.Id, text = "A reply."
        });
        var reply = await replyResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Attempt to update the reply
        var updateResponse = await _client.PutAsJsonAsync($"/api/posts/{reply!.Id}", new
        {
            text = "Should not work."
        });

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    // ---- Resolve / Pending Review ----

    [Fact]
    public async Task Create_Reply_ResolveSetssPendingReview()
    {
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "New",
            title = "Resolve Test", text = "Will be resolved."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Resolve",
            actionForId = root!.Id, text = "Work complete."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Pending Review", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_ReopenFromPendingReview()
    {
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "New",
            title = "Reopen PR Test", text = "Will be resolved then reopened."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Resolve",
            actionForId = root!.Id, text = "Work complete."
        });

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "Reopen",
            actionForId = root.Id, text = "Not satisfied, reopening."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task GetAll_FilterByPendingReview()
    {
        // Seed data has a "Pending Review" post from Thread 6
        var posts = await _client.GetFromJsonAsync<List<PostDto>>(
            "/api/posts?status=Pending Review&projectId=1", JsonOptions);

        Assert.NotNull(posts);
        Assert.True(posts.Count > 0);
        Assert.All(posts, p => Assert.Equal("Pending Review", p.Status));
    }

    // ---- Archive enforcement ----

    [Fact]
    public async Task Create_Reply_AdminCanArchiveAnyIssue()
    {
        // Claude (AI, actor 1) creates issue; Human (Admin, actor 2) archives it
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Admin Archive Test", text = "Claude's issue."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var archiveResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "Archive",
            actionForId = root!.Id, text = "Admin closing."
        });

        Assert.Equal(HttpStatusCode.Created, archiveResponse.StatusCode);

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Closed", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_NonOwnerNonAdminArchiveRejected()
    {
        // Human (Admin, actor 2) creates issue; Claude (AI, actor 1) tries to archive → 403
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "New",
            title = "Reject Archive Test", text = "Human's issue."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var archiveResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Archive",
            actionForId = root!.Id, text = "Claude trying to close."
        });

        Assert.Equal(HttpStatusCode.Forbidden, archiveResponse.StatusCode);

        // Verify status unchanged
        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_DelegateCanArchive()
    {
        // Human (actor 2) creates issue, then resolves with ToActorId=1 (Claude as delegate)
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "New",
            title = "Delegate Archive Test", text = "Human's issue, will delegate."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Owner resolves and delegates to Claude
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 2, actionType = "Resolve",
            actionForId = root!.Id, toActorId = 1, text = "Delegating review to Claude."
        });

        // Claude (delegate) archives
        var archiveResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Archive",
            actionForId = root.Id, text = "Delegate closing."
        });

        Assert.Equal(HttpStatusCode.Created, archiveResponse.StatusCode);

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal("Closed", updated!.Status);
    }

    [Fact]
    public async Task Create_Reply_ToActorIdPropagatedToRoot()
    {
        var rootResponse = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Propagation Test", text = "Test ToActorId propagation."
        });
        var root = await rootResponse.Content.ReadFromJsonAsync<PostDto>(JsonOptions);
        Assert.Null(root!.ToActorId);

        // Post a Resolve with ToActorId
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Resolve",
            actionForId = root.Id, toActorId = 2, text = "Assigning to Human."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{root.Id}", JsonOptions);
        Assert.Equal(2, updated!.ToActorId);
    }

    // ---- DTOs ----

    private record PostDto(
        int Id, int ProjectId, int SessionId, string? Title,
        DateTime DateTime, int FromActorId, string? FromActor,
        int? ToActorId, string? ToActor, string ActionType,
        int? ActionForId, string? Status, string? Tags, string Text);

    private record PagedResponse(List<PostDto> Items, int TotalCount);
}
