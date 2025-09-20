using OrderProvision.Models;

namespace OrderProvision.Services.Interfaces
{
    public interface IProductHandlerFactory
    {
        IProductHandler GetHandler(ProductType productType);
    }
}