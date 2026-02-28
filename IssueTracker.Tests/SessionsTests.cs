using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IssueTracker.Tests;

public class SessionsTests : IClassFixture<IssueTrackerFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SessionsTests(IssueTrackerFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsSeededSessions()
    {
        var sessions = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions", JsonOptions);

        Assert.NotNull(sessions);
        Assert.True(sessions.Count >= 3);
    }

    [Fact]
    public async Task GetAll_FilterByProject()
    {
        var sessions = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions?projectId=1", JsonOptions);

        Assert.NotNull(sessions);
        Assert.All(sessions, s => Assert.Equal(1, s.ProjectId));
    }

    [Fact]
    public async Task GetById_ReturnsSession()
    {
        var session = await _client.GetFromJsonAsync<SessionDto>("/api/sessions/1", JsonOptions);

        Assert.NotNull(session);
        Assert.Equal(1, session.Id);
        Assert.Equal("Issue Tracker", session.ProjectName);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/sessions",
            new { projectId = 1, name = "Test Session" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Test Session", created.Name);
        Assert.Equal(1, created.ProjectId);
    }

    [Fact]
    public async Task Archive_And_Restore()
    {
        // Create a session to archive
        var createResponse = await _client.PostAsJsonAsync("/api/sessions",
            new { projectId = 1, name = "Archivable Session" });
        var created = await createResponse.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);

        // Archive it
        var archiveResponse = await _client.PutAsync($"/api/sessions/{created!.Id}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        // Default list should exclude it
        var sessions = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions?projectId=1", JsonOptions);
        Assert.DoesNotContain(sessions!, s => s.Id == created.Id);

        // Include archived should show it
        var allSessions = await _client.GetFromJsonAsync<List<SessionDto>>(
            "/api/sessions?projectId=1&includeArchived=true", JsonOptions);
        var archived = allSessions!.Single(s => s.Id == created.Id);
        Assert.True(archived.IsArchived);

        // Restore it
        var restoreResponse = await _client.PutAsync($"/api/sessions/{created.Id}/restore", null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        // Should be back in default list
        sessions = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions?projectId=1", JsonOptions);
        Assert.Contains(sessions!, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Archive_NotFound_Returns404()
    {
        var response = await _client.PutAsync("/api/sessions/999/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record SessionDto(
        int Id, string Name, int ProjectId, string? ProjectName,
        DateTime StartDate, DateTime CreatedOn, bool IsArchived, int PostCount);
}
