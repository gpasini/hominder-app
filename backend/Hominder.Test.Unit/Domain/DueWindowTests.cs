using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class DueWindowTests
{
    [Fact]
    public void Construct_WithOpenAfterDue_Throws()
    {
        var open = new DateOnly(2026, 5, 10);
        var due = new DateOnly(2026, 5, 1);

        Assert.Throws<DomainException>(() => new DueWindow(open, due));
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new DueWindow(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));
        var b = new DueWindow(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));

        Assert.Equal(a, b);
    }
}
