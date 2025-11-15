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
        services.AddScoped<IElasticIndexService, ElasticIndexService>();

        // Repository registrations
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddAutoMapper(cfg => { }, typeof(InfrastructureAutoMapperProfile).Assembly);
        services.AddScoped<IProductAndDiscountRepository, ProductAndDiscountRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();

        return services;
    }

    public static IServiceCollection AddMongoInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // MongoDB settings
        var mongoDbSettingsSection = configuration.GetSection("MongoDbInfra");
        services.Configure<MongoDbSettings>(mongoDbSettingsSection);

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var mongoDbSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            // Enable logging if requested
            if (isDevelopment)
            {
                mongoDbSettings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(e =>
                    {
                        Console.WriteLine($"Mongo Command Started: {e.CommandName} - {e.Command.ToJson()}");
                    });
                };
            }

            return new MongoClient(mongoDbSettings);
        });

        services.AddScoped(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.Database);
        });

        return services;
    }
    
    public static IServiceCollection AddElasticInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var elasticUrl = configuration["ElasticSearch:NodeUrl"];
        var elasticIndex = configuration["ElasticSearch:Index"];

        services.AddSingleton<IElasticClient>(sp =>
        {
            var uri = new Uri(elasticUrl);
            var settings = new ConnectionSettings(uri)
                .DefaultIndex(elasticIndex)
                // Map ProductEntity.Id as document Id for NEST;
                .DefaultMappingFor<ProductEntity>(m => m.IdProperty(p => p.Id)
                .PropertyName(p => p.Name, "name"))
                // Required additions for ES 8.x stability:
                .DisableDirectStreaming()                      // helpful debugging
                .RequestTimeout(TimeSpan.FromSeconds(60))      // ES operations can be slow at startup
                .PingTimeout(TimeSpan.FromSeconds(30))         // avoid premature ping failures
                .SniffOnStartup(false)                         // disable sniffing (not needed for single-node)
                .SniffOnConnectionFault(false)
                .EnableApiVersioningHeader()                   // recommended for ES 8+
                .ServerCertificateValidationCallback((o, cert, chain, errors) => true); // allow self-signed certs
                
            #if DEBUG
                settings.DisableDirectStreaming();
            #endif
            
            return new ElasticClient(settings);
        });

        return services;
    }
}