using Catalog.API.Models.Products.GetProducts;
using FluentValidation;

namespace Catalog.API.Validators
{
    public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
    {
        public GetProductsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("El numero de pagina debe ser mayor a cero.");
            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("El tamaño de paina debe estar entre 1 y 100");
        }
    }
}
