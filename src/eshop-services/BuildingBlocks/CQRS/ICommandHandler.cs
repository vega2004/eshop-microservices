using MediatR;

namespace BuildingBlocks.CQRS;

    public interface ICommandHandler<in ICommand> : ICommandHandler<ICommand, Unit>
    where ICommand : ICommand<Unit>
    {
    }

   public interface ICommandHandler<in ICommand, TResponse> : IRequestHandler<ICommand, TResponse>
    where ICommand : ICommand<TResponse>
    where TResponse : notnull
    {

    }   

