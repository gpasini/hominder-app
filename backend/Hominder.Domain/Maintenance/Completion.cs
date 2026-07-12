using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Domain.Maintenance;

public sealed class Completion : ValueObject
{
    public Completion(DateOnly completedOn, HouseholdMemberId completedBy)
    {
        CompletedOn = completedOn;
        CompletedBy = completedBy;
    }

    public DateOnly CompletedOn { get; }

    public HouseholdMemberId CompletedBy { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CompletedOn;
        yield return CompletedBy;
    }
}
