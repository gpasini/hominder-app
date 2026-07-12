using Hominder.Domain.Common;

namespace Hominder.Domain.Household;

public sealed record HouseholdMemberDeletedDomainEvent(
    HouseholdMemberId MemberId,
    DateTime OccurredOnUtc) : IDomainEvent;
