using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Queries;

public sealed record GetMaintenanceTasksQuery : IQuery<IReadOnlyList<MaintenanceTaskView>>;

public sealed class GetMaintenanceTasksHandler
    : IRequestHandler<GetMaintenanceTasksQuery, IReadOnlyList<MaintenanceTaskView>>
{
    private readonly IMaintenanceTaskRepository _tasks;
    private readonly IHouseholdMemberRepository _members;
    private readonly TimeProvider _timeProvider;

    public GetMaintenanceTasksHandler(
        IMaintenanceTaskRepository tasks,
        IHouseholdMemberRepository members,
        TimeProvider timeProvider)
    {
        _tasks = tasks;
        _members = members;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<MaintenanceTaskView>> Handle(
        GetMaintenanceTasksQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var tasks = await _tasks.GetAllAsync(cancellationToken);
        var names = (await _members.GetAllAsync(cancellationToken))
            .ToDictionary(member => member.Id, member => member.Name);

        return tasks
            .Select(task => ToView(task, today, names))
            .OrderBy(view => UrgencyRank(view.Status))
            .ThenBy(view => view.DueDate)
            .ToList();
    }

    private static MaintenanceTaskView ToView(
        MaintenanceTask task, DateOnly today, IReadOnlyDictionary<HouseholdMemberId, string> names)
    {
        var evaluation = task.Evaluate(today);
        var assigneeName = task.AssigneeId is HouseholdMemberId id && names.TryGetValue(id, out var name)
            ? name
            : null;

        return new MaintenanceTaskView(
            task.Id.Value,
            task.Title,
            task.Notes,
            evaluation.Status.ToString(),
            evaluation.Window.OpenDate,
            evaluation.Window.DueDate,
            evaluation.DaysOverdue,
            task.AssigneeId?.Value,
            assigneeName,
            task.Policy.RequiresNextDueOverride);
    }

    private static int UrgencyRank(string status) => status switch
    {
        nameof(MaintenanceStatus.Overdue) => 0,
        nameof(MaintenanceStatus.Due) => 1,
        nameof(MaintenanceStatus.Upcoming) => 2,
        _ => 3,
    };
}
