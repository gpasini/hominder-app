using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteInTransactionAsync(() => next(), cancellationToken);
}
