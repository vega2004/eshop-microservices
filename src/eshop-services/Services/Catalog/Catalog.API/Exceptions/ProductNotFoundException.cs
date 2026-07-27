namespace Catalog.API.Exceptions
{
    public class ProductNotFoundException(Guid id)
        : Exception($"El producto con identificador {id} no fue encontrado.")
    {
    }
}
