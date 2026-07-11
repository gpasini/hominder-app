using Hominder.Application.Common.Exceptions;
using Hominder.Application.Household.Commands;
using Hominder.Application.Household.Queries;
using Hominder.Domain.Household;
using Hominder.Test.Unit.Application.Fakes;

namespace Hominder.Test.Unit.Application;

public class HouseholdMemberFeaturesTests
{
    [Fact]
    public async Task Create_PersistsMember()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        var handler = new CreateHouseholdMemberHandler(repository);

        var id = await handler.Handle(new CreateHouseholdMemberCommand("Grégory"), CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(id, repository.Items[0].Id.Value);
    }

    [Fact]
    public async Task Delete_UnknownMember_Throws()
    {
        var handler = new DeleteHouseholdMemberHandler(new InMemoryHouseholdMemberRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new DeleteHouseholdMemberCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Get_ReturnsAllMembers()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        repository.Items.Add(HouseholdMember.Create("Grégory"));
        var handler = new GetHouseholdMembersHandler(repository);

        var result = await handler.Handle(new GetHouseholdMembersQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Grégory", result[0].Name);
    }
}
