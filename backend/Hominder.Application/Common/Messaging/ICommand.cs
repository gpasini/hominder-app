using MediatR;

namespace Hominder.Application.Common.Messaging;

public interface ICommand : IRequest, IBaseCommand;

public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand;
