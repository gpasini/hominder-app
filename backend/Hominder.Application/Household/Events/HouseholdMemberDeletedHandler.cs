using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Events;

public sealed class HouseholdMemberDeletedHandler : INotificationHandler<HouseholdMemberDeletedDomainEvent>
{
    private readonly IMaintenanceTaskRepository _tasks;

    public HouseholdMemberDeletedHandler(IMaintenanceTaskRepository tasks) => _tasks = tasks;

    public async Task Handle(HouseholdMemberDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var tasks = await _tasks.GetAllAsync(cancellationToken);
        foreach (var task in tasks.Where(task => task.AssigneeId == notification.MemberId))
        {
            task.Unassign();
            _tasks.Save(task);
        }
    }
}
