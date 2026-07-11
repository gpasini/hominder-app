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
    private readonly IMaintenanceTaskRepository _repository;

    public MarkMaintenanceTaskDoneHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(MarkMaintenanceTaskDoneCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        task.MarkDone(request.CompletedOn, new HouseholdMemberId(request.CompletedBy), request.NextDueOverride);
    }
}
