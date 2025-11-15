using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevOpsDemo.Infrastructure;
using DevOpsDemo.Interfaces;

namespace DevOpsDemo.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            // Call Infrastructure DI
            services.AddInfrastructureServices(configuration, isDevelopment);
            services.AddMongoInfrastructureServices(configuration, isDevelopment);
            services.AddElasticInfrastructureServices(configuration, isDevelopment);

            // Application services
            services.AddScoped<IProductService, ProductService>();
            services.AddAutoMapper(cfg => { }, typeof(ApplicationAutoMapperProfile).Assembly);
            services.AddScoped<IProductAndDiscountService, ProductAndDiscountService>();
            services.AddScoped<ISalesService, SalesService>();

            return services;
        }
    }
}
