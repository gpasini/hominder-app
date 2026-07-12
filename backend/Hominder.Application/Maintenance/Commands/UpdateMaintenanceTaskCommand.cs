using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record UpdateMaintenanceTaskCommand(
    Guid TaskId,
    string Title,
    string? Notes,
    RecurrencePolicyInput Policy,
    Guid? AssigneeId) : ICommand;

public sealed class UpdateMaintenanceTaskHandler : IRequestHandler<UpdateMaintenanceTaskCommand>
{
    private readonly IMaintenanceTaskRepository _repository;

    public UpdateMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(UpdateMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        var policy = RecurrencePolicyFactory.Create(request.Policy);
        var assignee = request.AssigneeId is Guid value ? new HouseholdMemberId(value) : (HouseholdMemberId?)null;
        task.Update(request.Title, request.Notes, policy, assignee);
        _repository.Save(task);
    }
}
