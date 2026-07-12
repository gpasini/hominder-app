using Hominder.Application.Common.Exceptions;
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Commands;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Test.Unit.Application.Fakes;

namespace Hominder.Test.Unit.Application;

public class MaintenanceTaskCommandsTests
{
    private static RecurrencePolicyInput SpringWindow() =>
        new(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

    private static RecurrencePolicyInput FixedDate(DateOnly due) =>
        new(RecurrenceKind.FixedDate, null, null, null, null, null, due);

    [Fact]
    public async Task Create_PersistsTaskAndReturnsId()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var handler = new CreateMaintenanceTaskHandler(repository);

        var id = await handler.Handle(
            new CreateMaintenanceTaskCommand("Tailler l'olivier", null, SpringWindow(), null),
            CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(id, repository.Items[0].Id.Value);
    }

    [Fact]
    public async Task Update_UnknownTask_Throws()
    {
        var handler = new UpdateMaintenanceTaskHandler(new InMemoryMaintenanceTaskRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateMaintenanceTaskCommand(Guid.NewGuid(), "x", null, SpringWindow(), null),
            CancellationToken.None));
    }

    [Fact]
    public async Task MarkDone_RecordsCompletion()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create("CT", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2026, 6, 30))), null);
        repository.Items.Add(task);
        var members = new InMemoryHouseholdMemberRepository();
        var member = HouseholdMember.Create("Grégory");
        members.Items.Add(member);
        var handler = new MarkMaintenanceTaskDoneHandler(repository, members);

        await handler.Handle(
            new MarkMaintenanceTaskDoneCommand(task.Id.Value, new DateOnly(2026, 6, 20), member.Id.Value, new DateOnly(2028, 6, 30)),
            CancellationToken.None);

        Assert.Single(task.Completions);
    }

    [Fact]
    public async Task MarkDone_UnknownCompletedBy_Throws()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create("CT", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2026, 6, 30))), null);
        repository.Items.Add(task);
        var handler = new MarkMaintenanceTaskDoneHandler(repository, new InMemoryHouseholdMemberRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new MarkMaintenanceTaskDoneCommand(task.Id.Value, new DateOnly(2026, 6, 20), Guid.NewGuid(), new DateOnly(2028, 6, 30)),
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create("x", null, RecurrencePolicyFactory.Create(SpringWindow()), null);
        repository.Items.Add(task);
        var handler = new DeleteMaintenanceTaskHandler(repository);

        await handler.Handle(new DeleteMaintenanceTaskCommand(task.Id.Value), CancellationToken.None);

        Assert.Empty(repository.Items);
    }
}
