using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Queries;

namespace Hominder.Test.Integration;

public class MaintenanceTaskEndpointsTests : IClassFixture<HominderApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HominderApiFactory _factory;

    public MaintenanceTaskEndpointsTests(HominderApiFactory factory) => _factory = factory;

    private sealed record CreateBody(string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

    private sealed record MarkDoneBody(DateOnly CompletedOn, Guid CompletedBy, DateOnly? NextDueOverride);

    private sealed record CreatedResponse(Guid Id);

    [Fact]
    public async Task CreateThenList_ReturnsTaskWithComputedStatus()
    {
        var client = _factory.CreateClient();
        var policy = new RecurrencePolicyInput(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

        var create = await client.PostAsJsonAsync(
            "/api/tasks", new CreateBody("Tailler l'olivier", null, policy, null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var tasks = await client.GetFromJsonAsync<List<MaintenanceTaskView>>("/api/tasks", JsonOptions);

        Assert.NotNull(tasks);
        Assert.Contains(tasks!, task => task.Title == "Tailler l'olivier");
    }

    [Fact]
    public async Task MarkDone_RecordsCompletion()
    {
        var client = _factory.CreateClient();
        var policy = new RecurrencePolicyInput(
            RecurrenceKind.FixedDate, null, null, null, null, null, new DateOnly(2026, 6, 30));

        var create = await client.PostAsJsonAsync(
            "/api/tasks", new CreateBody("Contrôle technique", null, policy, null));
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var mark = await client.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/completions",
            new MarkDoneBody(new DateOnly(2026, 6, 20), Guid.NewGuid(), new DateOnly(2028, 6, 30)));

        Assert.Equal(HttpStatusCode.NoContent, mark.StatusCode);
    }
}
