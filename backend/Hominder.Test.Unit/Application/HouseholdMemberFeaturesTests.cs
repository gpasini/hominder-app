using Hominder.Application.Common.Exceptions;
using Hominder.Application.Household.Commands;
using Hominder.Application.Household.Queries;
using Hominder.Application.Maintenance;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;
using Hominder.Test.Unit.Application.Fakes;

namespace Hominder.Test.Unit.Application;

public class HouseholdMemberFeaturesTests
{
    private static RecurrencePolicyInput SpringWindow() =>
        new(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

    [Fact]
    public async Task Create_PersistsMember()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        var handler = new CreateHouseholdMemberHandler(repository);

        var id = await handler.Handle(new CreateHouseholdMemberCommand("Grégory"), CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(id, repository.Items[0].Id.Value);
    }

    [Fact]
    public async Task Delete_UnknownMember_Throws()
    {
        var handler = new DeleteHouseholdMemberHandler(
            new InMemoryHouseholdMemberRepository(), new InMemoryMaintenanceTaskRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new DeleteHouseholdMemberCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_UnassignsTasksAndRemovesMember()
    {
        var members = new InMemoryHouseholdMemberRepository();
        var member = HouseholdMember.Create("Grégory");
        members.Items.Add(member);

        var tasks = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create(
            "Tailler l'olivier", null, RecurrencePolicyFactory.Create(SpringWindow()), member.Id);
        tasks.Items.Add(task);

        var handler = new DeleteHouseholdMemberHandler(members, tasks);

        await handler.Handle(new DeleteHouseholdMemberCommand(member.Id.Value), CancellationToken.None);

        Assert.Empty(members.Items);
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public async Task Get_ReturnsAllMembers()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        repository.Items.Add(HouseholdMember.Create("Grégory"));
        var handler = new GetHouseholdMembersHandler(repository);

        var result = await handler.Handle(new GetHouseholdMembersQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Grégory", result[0].Name);
    }
}
