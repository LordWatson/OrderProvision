using OrderProvision.Models;
using OrderProvision.Services.Interfaces;
using OrderProvision.Services.ProductHandlers;

namespace OrderProvision.Services
{
    /*
     * we're basically mapping a specific Handler class to a product type here
     * so this is how the code sends us to the HandsetHandler instead of the RouterHandler when we're using a Handet product
     */
    public class ProductHandlerFactory : IProductHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<ProductType, Type> _handlerTypes;

        public ProductHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _handlerTypes = new Dictionary<ProductType, Type>
            {
                { ProductType.Router, typeof(RouterHandler) },
                { ProductType.BroadbandLine, typeof(BroadbandLineHandler) },
                { ProductType.Handset, typeof(HandsetHandler) }
            };
        }

        public IProductHandler GetHandler(ProductType productType)
        {
            if (!_handlerTypes.TryGetValue(productType, out var handlerType))
            {
                throw new NotSupportedException($"No handler registered for product type: {productType}");
            }

            return (IProductHandler)_serviceProvider.GetRequiredService(handlerType);
        }
    }
}