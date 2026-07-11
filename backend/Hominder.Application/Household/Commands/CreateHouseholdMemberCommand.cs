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

    public async Task<Guid> Handle(CreateHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var member = HouseholdMember.Create(request.Name);
        await _repository.AddAsync(member, cancellationToken);
        return member.Id.Value;
    }
}
