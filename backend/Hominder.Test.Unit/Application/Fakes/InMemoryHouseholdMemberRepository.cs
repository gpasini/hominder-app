using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;

namespace Hominder.Test.Unit.Application.Fakes;

public sealed class InMemoryHouseholdMemberRepository : IHouseholdMemberRepository
{
    public List<HouseholdMember> Items { get; } = [];

    public Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default)
    {
        Items.Add(member);
        return Task.CompletedTask;
    }

    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(member => member.Id == id));

    public Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HouseholdMember>>(Items);

    public void Remove(HouseholdMember member) => Items.Remove(member);
}
