

using MediatR;
using System.Windows.Input;

namespace BuildingBlocks.CQRS;

public interface ICommand : ICommand<Unit>
{

}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
