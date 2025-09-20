using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevOpsDemo.Infrastructure;
using DevOpsDemo.Interfaces;

namespace DevOpsDemo.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Call Infrastructure DI
            services.AddInfrastructureServices(configuration);

            // Application services
            services.AddScoped<IProductService, ProductService>();
            services.AddAutoMapper(cfg => { }, typeof(ApplicationProfile).Assembly);

            return services;
        }
    }
}
