using System.Net.Http.Json;
using System.Text.Json;

namespace IssueTracker.Tests;

public class ActorsTests : IClassFixture<IssueTrackerFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ActorsTests(IssueTrackerFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsSeededActors()
    {
        var actors = await _client.GetFromJsonAsync<List<ActorDto>>("/api/actors", JsonOptions);

        Assert.NotNull(actors);
        Assert.Equal(3, actors.Count);
        Assert.Contains(actors, a => a.Name == "Claude");
        Assert.Contains(actors, a => a.Name == "Human");
        Assert.Contains(actors, a => a.Name == "System");
    }

    private record ActorDto(int Id, string Name);
}
