using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Infrastructure.Persistence;
using MediatR;
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

        using var context = new HominderDbContext(options, new NoOpPublisher());
        var entity = context.Model.FindEntityType(typeof(MaintenanceTask));

        Assert.NotNull(entity);
        Assert.NotNull(context.Model.FindEntityType(typeof(HouseholdMember)));
        Assert.Equal("jsonb", entity!.FindProperty(nameof(MaintenanceTask.Policy))!.GetColumnType());
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
