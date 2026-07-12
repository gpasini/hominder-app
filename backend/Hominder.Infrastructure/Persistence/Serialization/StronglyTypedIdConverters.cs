using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hominder.Infrastructure.Persistence.Serialization;

public sealed class MaintenanceTaskIdConverter : ValueConverter<MaintenanceTaskId, Guid>
{
    public MaintenanceTaskIdConverter()
        : base(id => id.Value, value => new MaintenanceTaskId(value))
    {
    }
}

public sealed class HouseholdMemberIdConverter : ValueConverter<HouseholdMemberId, Guid>
{
    public HouseholdMemberIdConverter()
        : base(id => id.Value, value => new HouseholdMemberId(value))
    {
    }
}
