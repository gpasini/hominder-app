using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record DeleteMaintenanceTaskCommand(Guid TaskId) : ICommand;

public sealed class DeleteMaintenanceTaskHandler : IRequestHandler<DeleteMaintenanceTaskCommand>
{
    private readonly IMaintenanceTaskRepository _repository;

    public DeleteMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(DeleteMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        _repository.Remove(task);
    }
}
