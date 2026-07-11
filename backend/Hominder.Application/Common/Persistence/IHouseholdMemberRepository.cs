using Hominder.Domain.Household;

namespace Hominder.Application.Common.Persistence;

public interface IHouseholdMemberRepository
{
    Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default);

    Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default);

    void Remove(HouseholdMember member);
}
