using Basket.API.Data;
using BuildingBlocks.CQRS;
using FluentValidation;

namespace Basket.API.Basket.DeleteBasket
{
    public record DeleteBasketCommand(string UserId)
        : ICommand<DeleteBasketResult>;

    public record DeleteBasketResult(bool IsSuccess);

    public class DeleteBasketCommandValidator
        : AbstractValidator<DeleteBasketCommand>
    {
        public DeleteBasketCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("El identificador de usuario es requerido");
        }
    }

    public class DeleteBasketCommandHandler(
        IBasketRepository repository)
        : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
    {
        public async Task<DeleteBasketResult> Handle(
            DeleteBasketCommand command,
            CancellationToken cancellationToken)
        {
            await repository.DeleteBasket(
                command.UserId,
                cancellationToken);

            return new DeleteBasketResult(true);
        }
    }
}
