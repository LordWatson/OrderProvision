using Microsoft.Extensions.Logging;
using OrderProvision.Models;
using OrderProvision.Services.Interfaces;

namespace OrderProvision.Services.ProductHandlers
{
    public class HandsetHandler : IProductHandler
    {
        private readonly ILogger<HandsetHandler> _logger;

        public ProductType ProductType => ProductType.Handset;

        public HandsetHandler(ILogger<HandsetHandler> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ProcessOrderAsync(OrderCreated orderCreated)
        {
            _logger.LogInformation("Processing handset order {OrderId}", orderCreated.order.id);
            
            // actions to provision a handset
            try
            {
                // 1. check model availability
                await CheckModelAvailabilityAsync(orderCreated.order);
                
                // 2. activate SIM card
                await ActivateSimCardAsync(orderCreated.order);
                
                // 3. configure device
                await ConfigureDeviceAsync(orderCreated.order);
                
                // 4. schedule delivery
                await ScheduleDeliveryAsync(orderCreated.order);
                
                _logger.LogInformation("handset order {OrderId} processed successfully", orderCreated.order.id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process handset order {OrderId}", orderCreated.order.id);
                return false;
            }
        }

        private async Task CheckModelAvailabilityAsync(Order order)
        {
            await Task.Delay(150);
            _logger.LogInformation("handset model availability checked for order {OrderId}", order.id);
        }

        private async Task ActivateSimCardAsync(Order order)
        {
            await Task.Delay(250);
            _logger.LogInformation("SIM card activated for handset order {OrderId}", order.id);
        }

        private async Task ConfigureDeviceAsync(Order order)
        {
            await Task.Delay(300);
            _logger.LogInformation("handset configured for order {OrderId}", order.id);
        }

        private async Task ScheduleDeliveryAsync(Order order)
        {
            await Task.Delay(100);
            _logger.LogInformation("Delivery scheduled for handset order {OrderId}", order.id);
        }
    }
}