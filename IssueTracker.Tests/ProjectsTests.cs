using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IssueTracker.Tests;

public class ProjectsTests : IClassFixture<IssueTrackerFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProjectsTests(IssueTrackerFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsSeededProjects()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", JsonOptions);

        Assert.NotNull(projects);
        Assert.True(projects.Count >= 2);
        Assert.Contains(projects, p => p.Name == "Issue Tracker");
        Assert.Contains(projects, p => p.Name == "Sample Project");
    }

    [Fact]
    public async Task GetById_ReturnsProject()
    {
        var project = await _client.GetFromJsonAsync<ProjectDto>("/api/projects/1", JsonOptions);

        Assert.NotNull(project);
        Assert.Equal(1, project.Id);
        Assert.Equal("Issue Tracker", project.Name);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/projects/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Test Project", created.Name);
    }

    [Fact]
    public async Task Update_ChangesName()
    {
        // Create a project to update (avoid mutating seeded data)
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new { name = "Before Rename" });
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);

        var updated = await _client.PutAsJsonAsync($"/api/projects/{created!.Id}", new { name = "After Rename" });
        var project = await updated.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);

        Assert.NotNull(project);
        Assert.Equal("After Rename", project.Name);

        // Verify via GET
        var fetched = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{created.Id}", JsonOptions);
        Assert.Equal("After Rename", fetched!.Name);
    }

    private record ProjectDto(int Id, string Name);
}
