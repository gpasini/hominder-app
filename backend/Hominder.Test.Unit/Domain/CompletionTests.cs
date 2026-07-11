using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class CompletionTests
{
    [Fact]
    public void Completions_WithSameValues_AreEqual()
    {
        var member = HouseholdMemberId.New();
        var date = new DateOnly(2026, 4, 18);

        Assert.Equal(new Completion(date, member), new Completion(date, member));
    }

    [Fact]
    public void CompletedEvent_ImplementsDomainEvent()
    {
        var domainEvent = new MaintenanceTaskCompletedDomainEvent(
            MaintenanceTaskId.New(),
            new DateOnly(2026, 4, 18),
            HouseholdMemberId.New(),
            DateTime.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(domainEvent);
    }
}
