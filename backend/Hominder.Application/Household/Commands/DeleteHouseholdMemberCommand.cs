using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Commands;

public sealed record DeleteHouseholdMemberCommand(Guid MemberId) : ICommand;

public sealed class DeleteHouseholdMemberHandler : IRequestHandler<DeleteHouseholdMemberCommand>
{
    private readonly IHouseholdMemberRepository _members;
    private readonly IMaintenanceTaskRepository _tasks;

    public DeleteHouseholdMemberHandler(IHouseholdMemberRepository members, IMaintenanceTaskRepository tasks)
    {
        _members = members;
        _tasks = tasks;
    }

    public async Task Handle(DeleteHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var memberId = new HouseholdMemberId(request.MemberId);
        var member = await _members.GetByIdAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Membre introuvable.");

        var tasks = await _tasks.GetAllAsync(cancellationToken);
        foreach (var task in tasks.Where(task => task.AssigneeId == memberId))
        {
            task.Unassign();
        }

        _members.Remove(member);
    }
}
