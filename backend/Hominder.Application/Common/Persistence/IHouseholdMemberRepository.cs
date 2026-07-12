using Hominder.Domain.Household;

namespace Hominder.Application.Common.Persistence;

public interface IHouseholdMemberRepository
{
    Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default);

    void Save(HouseholdMember member);

    void Remove(HouseholdMember member);
}
