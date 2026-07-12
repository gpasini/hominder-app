using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class HouseholdMemberRepository : IHouseholdMemberRepository
{
    private readonly HominderDbContext _context;

    public HouseholdMemberRepository(HominderDbContext context) => _context = context;

    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default) =>
        _context.HouseholdMembers.FirstOrDefaultAsync(member => member.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.HouseholdMembers.ToListAsync(cancellationToken);

    public void Save(HouseholdMember member)
    {
        if (_context.Entry(member).State == EntityState.Detached)
        {
            _context.HouseholdMembers.Add(member);
        }
    }

    public void Remove(HouseholdMember member) => _context.HouseholdMembers.Remove(member);
}
