using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace IssueTracker.Tests;

public class ProjectExportTests : IClassFixture<IssueTrackerFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProjectExportTests(IssueTrackerFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task ExportSqlite_NonExistentProject_Returns404()
    {
        var response = await _client.GetAsync("/api/projects/9999/export/sqlite");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportSqlite_ExistingProject_Returns200WithSqliteFile()
    {
        var response = await _client.GetAsync("/api/projects/1/export/sqlite");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("project-1-export.sqlite", response.Content.Headers.ContentDisposition?.FileName);

        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 0);
        // SQLite files start with "SQLite format 3\0"
        Assert.Equal("SQLite format 3\0", System.Text.Encoding.ASCII.GetString(content, 0, 16));
    }

    [Fact]
    public async Task ExportSqlite_ContainsCorrectData()
    {
        // Create a dedicated project with known data
        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new { name = "Export Test Project" });
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);

        var sessionResponse = await _client.PostAsJsonAsync("/api/sessions", new { ProjectId = project!.Id, Name = "Export Test Session" });
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);

        await _client.PostAsJsonAsync("/api/posts", new
        {
            SessionId = session!.Id,
            FromActorId = 1,
            ActionType = "New",
            Title = "Export Test Issue",
            Tags = "test",
            Text = "This issue should appear in the export."
        });

        await _client.PostAsJsonAsync("/api/posts", new
        {
            SessionId = session.Id,
            FromActorId = 2,
            ActionType = "New",
            Title = "Second Export Issue",
            Tags = "test",
            Text = "Another issue for the export."
        });

        // Export the project
        var response = await _client.GetAsync($"/api/projects/{project.Id}/export/sqlite");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Save to a temp file and query it
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var tempFile = Path.Combine(Path.GetTempPath(), $"export-test-{Guid.NewGuid():N}.sqlite");
        try
        {
            await File.WriteAllBytesAsync(tempFile, bytes);

            await using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync();

            // Verify project
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"Name\" FROM \"Projects\" WHERE \"Id\" = @id";
            cmd.Parameters.AddWithValue("@id", project.Id);
            var projectName = (string?)await cmd.ExecuteScalarAsync();
            Assert.Equal("Export Test Project", projectName);

            // Verify sessions
            cmd.CommandText = "SELECT COUNT(*) FROM \"Sessions\"";
            cmd.Parameters.Clear();
            var sessionCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, sessionCount);

            // Verify posts
            cmd.CommandText = "SELECT COUNT(*) FROM \"Posts\"";
            var postCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(2, postCount);

            // Verify actors (should only include referenced actors: 1 and 2)
            cmd.CommandText = "SELECT COUNT(*) FROM \"Actors\"";
            var actorCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(2, actorCount);

            // Verify specific post content
            cmd.CommandText = "SELECT \"Title\" FROM \"Posts\" ORDER BY \"Id\"";
            await using var reader = await cmd.ExecuteReaderAsync();
            var titles = new List<string>();
            while (await reader.ReadAsync())
                titles.Add(reader.GetString(0));
            Assert.Equal(["Export Test Issue", "Second Export Issue"], titles);
        }
        finally
        {
            SqliteConnection.ClearPool(new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly"));
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportSqlite_EmptyProject_ReturnsValidEmptyDb()
    {
        // Create a project with no sessions or posts
        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new { name = "Empty Export Project" });
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);

        var response = await _client.GetAsync($"/api/projects/{project!.Id}/export/sqlite");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var tempFile = Path.Combine(Path.GetTempPath(), $"export-empty-{Guid.NewGuid():N}.sqlite");
        try
        {
            await File.WriteAllBytesAsync(tempFile, bytes);

            await using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync();

            // Schema should exist with zero rows
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM \"Posts\"";
            var postCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(0, postCount);

            cmd.CommandText = "SELECT COUNT(*) FROM \"Sessions\"";
            var sessionCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(0, sessionCount);

            // Project record should be present
            cmd.CommandText = "SELECT COUNT(*) FROM \"Projects\"";
            var projectCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, projectCount);
        }
        finally
        {
            SqliteConnection.ClearPool(new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly"));
            File.Delete(tempFile);
        }
    }

    private record ProjectDto(int Id, string Name);
    private record SessionDto(int Id, string Name, int ProjectId);
}
