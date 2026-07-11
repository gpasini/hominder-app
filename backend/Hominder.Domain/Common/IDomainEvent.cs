using MediatR;

namespace Hominder.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}
