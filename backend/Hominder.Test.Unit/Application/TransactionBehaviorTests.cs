using Hominder.Application.Common.Behaviors;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Test.Unit.Application;

public class TransactionBehaviorTests
{
    private sealed record FakeCommand : ICommand;

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool Executed { get; private set; }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return await operation();
        }
    }

    [Fact]
    public async Task Handle_RunsHandlerInsideTransaction()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<FakeCommand, MediatR.Unit>(unitOfWork);
        var handlerRan = false;

        var result = await behavior.Handle(
            new FakeCommand(),
            _ =>
            {
                handlerRan = true;
                return Task.FromResult(MediatR.Unit.Value);
            },
            CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.True(handlerRan);
        Assert.Equal(MediatR.Unit.Value, result);
    }
}
