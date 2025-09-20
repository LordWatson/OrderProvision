using Microsoft.Extensions.Logging;
using OrderProvision.Models;
using OrderProvision.Services.Interfaces;

namespace OrderProvision.Services.ProductHandlers
{
    public class RouterHandler : IProductHandler
    {
        private readonly ILogger<RouterHandler> _logger;

        public ProductType ProductType => ProductType.Router;

        public RouterHandler(ILogger<RouterHandler> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ProcessOrderAsync(OrderCreated orderCreated)
        {
            _logger.LogInformation("Processing router order {OrderId}", orderCreated.order.id);
            
            // actions required to provision a router
            try
            {
                // 1. check inventory availability
                await CheckInventoryAsync(orderCreated.order);
                
                // 2. reserve router inventory
                await ReserveRouterAsync(orderCreated.order);
                
                // 3. schedule shipping
                await ScheduleShippingAsync(orderCreated.order);
                
                _logger.LogInformation("Router order {OrderId} processed successfully", orderCreated.order.id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process router order {OrderId}", orderCreated.order.id);
                return false;
            }
        }

        private async Task CheckInventoryAsync(Order order)
        {
            // simulate inventory check
            await Task.Delay(100);
            _logger.LogInformation("Inventory check completed for router order {OrderId}", order.id);
        }

        private async Task ReserveRouterAsync(Order order)
        {
            // simulate router reservation
            await Task.Delay(200);
            _logger.LogInformation("Router reserved for order {OrderId}", order.id);
        }

        private async Task ScheduleShippingAsync(Order order)
        {
            // simulate shipping scheduling
            await Task.Delay(150);
            _logger.LogInformation("Shipping scheduled for router order {OrderId}", order.id);
        }
    }
}