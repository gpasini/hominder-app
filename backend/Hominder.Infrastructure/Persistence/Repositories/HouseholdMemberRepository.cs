using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class HouseholdMemberRepository : IHouseholdMemberRepository
{
    private readonly HominderDbContext _context;

    public HouseholdMemberRepository(HominderDbContext context) => _context = context;

    public async Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default) =>
        await _context.HouseholdMembers.AddAsync(member, cancellationToken);

    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default) =>
        _context.HouseholdMembers.FirstOrDefaultAsync(member => member.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.HouseholdMembers.ToListAsync(cancellationToken);

    public void Remove(HouseholdMember member) => _context.HouseholdMembers.Remove(member);
}
