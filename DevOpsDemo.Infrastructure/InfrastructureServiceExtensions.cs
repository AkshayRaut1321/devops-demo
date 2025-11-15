using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using DevOpsDemo.Infrastructure.DomainImplementation;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using Nest;
using DevOpsDemo.Infrastructure.Entities;
using DevOpsDemo.Infrastructure.Interfaces;
using DevOpsDemo.Infrastructure.Implementation;

namespace DevOpsDemo.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // MongoDB settings
        var mongoDbSettingsSection = configuration.GetSection("MongoDb");
        services.Configure<MongoDbSettings>(mongoDbSettingsSection);

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var mongoSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            // Enable logging if requested
            if (isDevelopment)
            {
                mongoSettings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(e =>
                    {
                        Console.WriteLine($"Mongo Command Started: {e.CommandName} - {e.Command.ToJson()}");
                    });
                };
            }
            
            return new MongoClient(mongoSettings);
        });

        services.AddScoped(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.Database);
        });

        var elasticUrl = configuration["Elastic:Url"];
        var elasticIndex = configuration["Elastic:Index"];

        services.AddSingleton<IElasticClient>(sp =>
        {
            var uri = new Uri(elasticUrl);
            var settings = new ConnectionSettings(uri)
                .DefaultIndex(elasticIndex)
                // Map ProductEntity.Id as document Id for NEST;
                .DefaultMappingFor<ProductEntity>(m => m.IdProperty(p => p.Id)
                .PropertyName(p => p.Name, "name"));
                
            #if DEBUG
                settings.DisableDirectStreaming();
            #endif
            
            return new ElasticClient(settings);
        });
        services.AddScoped<IElasticIndexService, ElasticIndexService>();

        // Repository registrations
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddAutoMapper(cfg => { }, typeof(InfrastructureAutoMapperProfile).Assembly);
        services.AddScoped<IProductAndDiscountRepository, ProductAndDiscountRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();

        return services;
    }
}