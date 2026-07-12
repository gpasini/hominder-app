using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Application.Household.Queries;

public sealed record GetHouseholdMembersQuery : IQuery<IReadOnlyList<HouseholdMemberView>>;

public sealed class GetHouseholdMembersHandler
    : IRequestHandler<GetHouseholdMembersQuery, IReadOnlyList<HouseholdMemberView>>
{
    private readonly IHouseholdMemberRepository _repository;

    public GetHouseholdMembersHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<HouseholdMemberView>> Handle(
        GetHouseholdMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _repository.GetAllAsync(cancellationToken);
        return members
            .Select(member => new HouseholdMemberView(member.Id.Value, member.Name))
            .OrderBy(view => view.Name)
            .ToList();
    }
}
