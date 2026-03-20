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

    // ---- BATON auto-archive ----

    [Fact]
    public async Task Create_BatonPost_AutoArchivesPreviousOpenBaton()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "BATON 1", tags = "baton", text = "First baton."
        });
        var baton1 = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var r2 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "BATON 2", tags = "baton", text = "Second baton."
        });
        var baton2 = await r2.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // First baton should now be Closed
        var updated1 = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{baton1!.Id}", JsonOptions);
        Assert.Equal("Closed", updated1!.Status);

        // Second baton should be Open
        Assert.Equal("Open", baton2!.Status);

        // Thread for first baton should contain a system Archive post
        var thread = await _client.GetFromJsonAsync<List<PostDto>>(
            $"/api/posts/{baton1.Id}/thread", JsonOptions);
        Assert.Contains(thread!, p => p.ActionType == "Archive" && p.FromActorId == 3);
    }

    [Fact]
    public async Task Create_BatonPost_AutoArchivesMultipleOpenBatons()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Multi-BATON 1", tags = "baton", text = "First."
        });
        var b1 = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var r2 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Multi-BATON 2", tags = "baton", text = "Second."
        });
        var b2 = await r2.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Reopen the first so we have two open batons
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Reopen",
            actionForId = b1!.Id, text = "Reopening."
        });

        // Create a third baton — should archive both
        var r3 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Multi-BATON 3", tags = "baton", text = "Third."
        });
        var b3 = await r3.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var u1 = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{b1.Id}", JsonOptions);
        var u2 = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{b2!.Id}", JsonOptions);
        Assert.Equal("Closed", u1!.Status);
        Assert.Equal("Closed", u2!.Status);
        Assert.Equal("Open", b3!.Status);
    }

    [Fact]
    public async Task Create_BatonPost_DoesNotArchiveDifferentProjectBaton()
    {
        // Create a baton in project 1
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Project1 BATON", tags = "baton", text = "Project 1 baton."
        });
        var baton1 = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Create a new project and session
        var projResponse = await _client.PostAsJsonAsync("/api/projects", new { name = "BatonTestProject" });
        var proj = await projResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);

        var sessResponse = await _client.PostAsJsonAsync("/api/sessions", new
        {
            projectId = proj!.Id, name = "Baton Test Session"
        });
        var sess = await sessResponse.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);

        // Create a baton in the new project
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = sess!.Id, fromActorId = 1, actionType = "New",
            title = "Project2 BATON", tags = "baton", text = "Project 2 baton."
        });

        // Project 1 baton should still be Open
        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{baton1!.Id}", JsonOptions);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task Create_BatonReply_DoesNotTriggerAutoArchive()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "BATON NoArchive", tags = "baton", text = "Should stay open."
        });
        var baton = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Reply with "baton" tag — should NOT trigger auto-archive
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Discuss",
            actionForId = baton!.Id, tags = "baton", text = "Discussion reply."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{baton.Id}", JsonOptions);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task Create_BatonPost_WithMultipleTags_AutoArchives()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Tagged BATON 1", tags = "baton,session-014", text = "First."
        });
        var baton1 = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Tagged BATON 2", tags = "baton,session-015", text = "Second."
        });

        var updated = await _client.GetFromJsonAsync<PostDto>($"/api/posts/{baton1!.Id}", JsonOptions);
        Assert.Equal("Closed", updated!.Status);
    }

    // ---- GET /api/posts/latest-baton ----

    [Fact]
    public async Task GetLatestBaton_ReturnsMostRecentBaton()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Latest Test 1", tags = "baton", text = "Older baton."
        });
        var baton1 = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var r2 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Latest Test 2", tags = "baton", text = "Newer baton."
        });
        var baton2 = await r2.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        var latest = await _client.GetFromJsonAsync<PostDto>(
            "/api/posts/latest-baton?projectId=1", JsonOptions);

        Assert.NotNull(latest);
        Assert.Equal(baton2!.Id, latest.Id);
    }

    [Fact]
    public async Task GetLatestBaton_NoProject_Returns404()
    {
        var response = await _client.GetAsync("/api/posts/latest-baton?projectId=9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestBaton_ReturnsClosedBatonIfLatest()
    {
        var r1 = await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "New",
            title = "Closed Baton Test", tags = "baton", text = "Will be closed."
        });
        var baton = await r1.Content.ReadFromJsonAsync<PostDto>(JsonOptions);

        // Manually archive it
        await _client.PostAsJsonAsync("/api/posts", new
        {
            sessionId = 1, fromActorId = 1, actionType = "Archive",
            actionForId = baton!.Id, text = "Manual close."
        });

        var latest = await _client.GetFromJsonAsync<PostDto>(
            "/api/posts/latest-baton?projectId=1", JsonOptions);

        Assert.NotNull(latest);
        // Should return the closed baton (it's the most recent by DateTime)
        Assert.Equal("Closed", latest.Status);
    }

    // ---- DTOs ----

    private record PostDto(
        int Id, int ProjectId, int SessionId, string? Title,
        DateTime DateTime, int FromActorId, string? FromActor,
        int? ToActorId, string? ToActor, string ActionType,
        int? ActionForId, string? Status, string? Tags, string Text);

    private record PagedResponse(List<PostDto> Items, int TotalCount);
    private record ProjectDto(int Id, string Name);
    private record SessionDto(int Id, string Name, int ProjectId);
}
