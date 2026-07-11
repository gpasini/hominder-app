using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hominder.Application.Household.Queries;
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Queries;

namespace Hominder.Test.Integration;

public class HouseholdMemberEndpointsTests : IClassFixture<HominderApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HominderApiFactory _factory;

    public HouseholdMemberEndpointsTests(HominderApiFactory factory) => _factory = factory;

    private sealed record CreateBody(string Name);

    private sealed record CreatedResponse(Guid Id);

    private sealed record CreateTaskBody(string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

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

    [Fact]
    public async Task Delete_UnassignsTasksAssignedToMember()
    {
        var client = _factory.CreateClient();

        var createMember = await client.PostAsJsonAsync("/api/members", new CreateBody("Jean"));
        var member = await createMember.Content.ReadFromJsonAsync<CreatedResponse>();

        var policy = new RecurrencePolicyInput(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);
        var createTask = await client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskBody("Nettoyer la gouttière", null, policy, member!.Id));
        var task = await createTask.Content.ReadFromJsonAsync<CreatedResponse>();

        var delete = await client.DeleteAsync($"/api/members/{member.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var tasks = await client.GetFromJsonAsync<List<MaintenanceTaskView>>("/api/tasks", JsonOptions);

        var reloaded = tasks!.Single(candidate => candidate.Id == task!.Id);
        Assert.Null(reloaded.AssigneeId);
    }
}
