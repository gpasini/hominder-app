using System.Net;
using System.Net.Http.Json;
using Hominder.Application.Household.Queries;

namespace Hominder.Test.Integration;

public class HouseholdMemberEndpointsTests : IClassFixture<HominderApiFactory>
{
    private readonly HominderApiFactory _factory;

    public HouseholdMemberEndpointsTests(HominderApiFactory factory) => _factory = factory;

    private sealed record CreateBody(string Name);

    private sealed record CreatedResponse(Guid Id);

    [Fact]
    public async Task CreateThenList_ReturnsMember()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/members", new CreateBody("Grégory"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var members = await client.GetFromJsonAsync<List<HouseholdMemberView>>("/api/members");

        Assert.NotNull(members);
        Assert.Contains(members!, member => member.Name == "Grégory");
    }
}
