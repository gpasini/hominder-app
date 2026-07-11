using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class StronglyTypedIdTests
{
    [Fact]
    public void New_ProducesDistinctValues()
    {
        var first = MaintenanceTaskId.New();
        var second = MaintenanceTaskId.New();

        Assert.NotEqual(first, second);
        Assert.NotEqual(Guid.Empty, first.Value);
    }

    [Fact]
    public void SameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        Assert.Equal(new MaintenanceTaskId(value), new MaintenanceTaskId(value));
    }
}
