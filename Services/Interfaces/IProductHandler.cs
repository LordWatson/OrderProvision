using OrderProvision.Models;

namespace OrderProvision.Services.Interfaces
{
    public interface IProductHandler
    {
        ProductType ProductType { get; }
        Task<bool> ProcessOrderAsync(OrderCreated orderCreated);
    }
}