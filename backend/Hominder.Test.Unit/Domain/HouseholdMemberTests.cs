using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Test.Unit.Domain;

public class HouseholdMemberTests
{
    [Fact]
    public void Create_TrimsName()
    {
        var member = HouseholdMember.Create("  Grégory  ");

        Assert.Equal("Grégory", member.Name);
        Assert.NotEqual(Guid.Empty, member.Id.Value);
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        Assert.Throws<DomainException>(() => HouseholdMember.Create("   "));
    }
}
