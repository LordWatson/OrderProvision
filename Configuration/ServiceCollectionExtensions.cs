using OrderProvision.Services;
using OrderProvision.Services.Interfaces;
using OrderProvision.Services.ProductHandlers;

namespace OrderProvision.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProductHandlers(this IServiceCollection services)
        {
            /*
             * we're basically binding our classes here in the same way we do in the Laravel Service Provider 
             */
            services.AddScoped<RouterHandler>();
            services.AddScoped<BroadbandLineHandler>();
            services.AddScoped<HandsetHandler>();
            
            services.AddScoped<IProductHandlerFactory, ProductHandlerFactory>();
            
            return services;
        }
    }
}