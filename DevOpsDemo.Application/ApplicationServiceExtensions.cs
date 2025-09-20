using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevOpsDemo.Infrastructure;

namespace DevOpsDemo.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Call Infrastructure DI
            services.AddInfrastructureServices(configuration);

            // Application services
            services.AddScoped<ProductService>();
            services.AddAutoMapper(typeof(ApplicationProfile).Assembly);

            return services;
        }
    }
}
