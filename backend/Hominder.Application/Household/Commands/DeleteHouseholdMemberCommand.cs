using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Commands;

public sealed record DeleteHouseholdMemberCommand(Guid MemberId) : ICommand;

public sealed class DeleteHouseholdMemberHandler : IRequestHandler<DeleteHouseholdMemberCommand>
{
    private readonly IHouseholdMemberRepository _repository;

    public DeleteHouseholdMemberHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public async Task Handle(DeleteHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _repository.GetByIdAsync(new HouseholdMemberId(request.MemberId), cancellationToken)
            ?? throw new NotFoundException("Membre introuvable.");
        _repository.Remove(member);
    }
}
