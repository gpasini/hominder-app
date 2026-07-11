using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Domain.Maintenance;

public sealed record MaintenanceTaskCompletedDomainEvent(
    MaintenanceTaskId TaskId,
    DateOnly CompletedOn,
    HouseholdMemberId CompletedBy,
    DateTime OccurredOnUtc) : IDomainEvent;
