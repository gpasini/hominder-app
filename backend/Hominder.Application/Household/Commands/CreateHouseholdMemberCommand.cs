using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Commands;

public sealed record CreateHouseholdMemberCommand(string Name) : ICommand<Guid>;

public sealed class CreateHouseholdMemberHandler : IRequestHandler<CreateHouseholdMemberCommand, Guid>
{
    private readonly IHouseholdMemberRepository _repository;

    public CreateHouseholdMemberHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public Task<Guid> Handle(CreateHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var member = HouseholdMember.Create(request.Name);
        _repository.Save(member);
        return Task.FromResult(member.Id.Value);
    }
}
