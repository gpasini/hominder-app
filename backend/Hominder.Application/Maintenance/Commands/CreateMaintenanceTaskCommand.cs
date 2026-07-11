using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record CreateMaintenanceTaskCommand(
    string Title,
    string? Notes,
    RecurrencePolicyInput Policy,
    Guid? AssigneeId) : ICommand<Guid>;

public sealed class CreateMaintenanceTaskHandler : IRequestHandler<CreateMaintenanceTaskCommand, Guid>
{
    private readonly IMaintenanceTaskRepository _repository;

    public CreateMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var policy = RecurrencePolicyFactory.Create(request.Policy);
        var assignee = request.AssigneeId is Guid value ? new HouseholdMemberId(value) : (HouseholdMemberId?)null;
        var task = MaintenanceTask.Create(request.Title, request.Notes, policy, assignee);
        await _repository.AddAsync(task, cancellationToken);
        return task.Id.Value;
    }
}
