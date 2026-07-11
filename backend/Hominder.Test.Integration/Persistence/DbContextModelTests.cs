using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Test.Integration.Persistence;

public class DbContextModelTests
{
    [Fact]
    public void Model_MapsAggregatesAndJsonbPolicy()
    {
        var options = new DbContextOptionsBuilder<HominderDbContext>()
            .UseNpgsql("Host=localhost;Database=hominder;Username=hominder;Password=hominder")
            .Options;

        using var context = new HominderDbContext(options);
        var entity = context.Model.FindEntityType(typeof(MaintenanceTask));

        Assert.NotNull(entity);
        Assert.NotNull(context.Model.FindEntityType(typeof(HouseholdMember)));
        Assert.Equal("jsonb", entity!.FindProperty(nameof(MaintenanceTask.Policy))!.GetColumnType());
    }
}
