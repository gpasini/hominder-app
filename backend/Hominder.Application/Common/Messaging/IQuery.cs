using MediatR;

namespace Hominder.Application.Common.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
