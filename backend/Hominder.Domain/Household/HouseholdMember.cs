using Hominder.Domain.Common;

namespace Hominder.Domain.Household;

public sealed class HouseholdMember : AggregateRoot<HouseholdMemberId>
{
    private HouseholdMember(HouseholdMemberId id, string name)
        : base(id) => Name = name;

    public string Name { get; private set; }

    public static HouseholdMember Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom du membre est obligatoire.");
        }

        return new HouseholdMember(HouseholdMemberId.New(), name.Trim());
    }

    public void Delete() =>
        RaiseDomainEvent(new HouseholdMemberDeletedDomainEvent(Id, DateTime.UtcNow));
}
