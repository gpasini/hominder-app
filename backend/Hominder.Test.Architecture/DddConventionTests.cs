using System.Reflection;
using Hominder.Domain.Common;
using NetArchTest.Rules;
using Xunit;

namespace Hominder.Test.Architecture;

public class DddConventionTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("Hominder.Domain");

    [Fact]
    public void DomainEvents_ShouldBeSealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void TypesNamedDomainEvent_ShouldImplementIDomainEvent()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("DomainEvent")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 } failing
            ? "Violating types: " + string.Join(", ", failing)
            : "Convention violated.";
}
