using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Queries;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Test.Unit.Application.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Hominder.Test.Unit.Application;

public class GetMaintenanceTasksHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);

    private static RecurrencePolicyInput FixedDate(DateOnly due) =>
        new(RecurrenceKind.FixedDate, null, null, null, null, null, due);

    [Fact]
    public async Task Handle_OrdersOverdueBeforeUpcoming_AndResolvesAssignee()
    {
        var tasks = new InMemoryMaintenanceTaskRepository();
        var members = new InMemoryHouseholdMemberRepository();
        var member = HouseholdMember.Create("Grégory");
        members.Items.Add(member);

        var upcoming = MaintenanceTask.Create(
            "Futur", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2027, 1, 1))), member.Id);
        var overdue = MaintenanceTask.Create(
            "En retard", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2026, 6, 1))), null);
        tasks.Items.Add(upcoming);
        tasks.Items.Add(overdue);

        var handler = new GetMaintenanceTasksHandler(tasks, members, new FakeTimeProvider(Now));

        var result = await handler.Handle(new GetMaintenanceTasksQuery(), CancellationToken.None);

        Assert.Equal("En retard", result[0].Title);
        Assert.Equal("Overdue", result[0].Status);
        Assert.Equal("Grégory", result[1].AssigneeName);
    }
}
