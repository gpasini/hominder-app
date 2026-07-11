using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record MarkMaintenanceTaskDoneCommand(
    Guid TaskId,
    DateOnly CompletedOn,
    Guid CompletedBy,
    DateOnly? NextDueOverride) : ICommand;

public sealed class MarkMaintenanceTaskDoneHandler : IRequestHandler<MarkMaintenanceTaskDoneCommand>
{
    private readonly IMaintenanceTaskRepository _tasks;
    private readonly IHouseholdMemberRepository _members;

    public MarkMaintenanceTaskDoneHandler(IMaintenanceTaskRepository tasks, IHouseholdMemberRepository members)
    {
        _tasks = tasks;
        _members = members;
    }

    public async Task Handle(MarkMaintenanceTaskDoneCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");

        var completedBy = new HouseholdMemberId(request.CompletedBy);
        _ = await _members.GetByIdAsync(completedBy, cancellationToken)
            ?? throw new NotFoundException("Membre introuvable.");

        task.MarkDone(request.CompletedOn, completedBy, request.NextDueOverride);
    }
}
