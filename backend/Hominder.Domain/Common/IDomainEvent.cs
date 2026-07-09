namespace Hominder.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
