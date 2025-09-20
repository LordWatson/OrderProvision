using Microsoft.Extensions.Logging;
using OrderProvision.Models;
using OrderProvision.Services.Interfaces;

namespace OrderProvision.Services.ProductHandlers
{
    public class BroadbandLineHandler : IProductHandler
    {
        private readonly ILogger<BroadbandLineHandler> _logger;

        public ProductType ProductType => ProductType.BroadbandLine;

        public BroadbandLineHandler(ILogger<BroadbandLineHandler> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ProcessOrderAsync(OrderCreated orderCreated)
        {
            _logger.LogInformation("Processing broadband line order {OrderId}", orderCreated.order.id);
            
            // actions to provision a broadband line
            try
            {
                // 1. check line availability
                await CheckLineAvailabilityAsync(orderCreated.order);
                
                // 2. schedule survey
                await ScheduleTechnicianAsync(orderCreated.order);
                
                // 3. provision line
                await ProvisionLineAsync(orderCreated.order);
                
                _logger.LogInformation("Broadband line order {OrderId} processed successfully", orderCreated.order.id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process broadband line order {OrderId}", orderCreated.order.id);
                return false;
            }
        }

        private async Task CheckLineAvailabilityAsync(Order order)
        {
            await Task.Delay(300);
            _logger.LogInformation("Line availability checked for order {OrderId}", order.id);
        }

        private async Task ScheduleTechnicianAsync(Order order)
        {
            await Task.Delay(500);
            _logger.LogInformation("Technician scheduled for broadband order {OrderId}", order.id);
        }

        private async Task ProvisionLineAsync(Order order)
        {
            await Task.Delay(200);
            _logger.LogInformation("Line provisioned for order {OrderId}", order.id);
        }
    }
}