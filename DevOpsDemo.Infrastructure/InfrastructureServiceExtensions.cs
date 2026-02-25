using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using DevOpsDemo.Infrastructure.DomainImplementation;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using DevOpsDemo.Infrastructure.Interfaces;
using DevOpsDemo.Infrastructure.Implementation;
using DevOpsDemo.Infrastructure.Entities.Config;
using DevOpsDemo.Infrastructure.Entities.Database;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace DevOpsDemo.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IElasticIndexService, ElasticIndexService>();

        // Repository registrations
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddAutoMapper(cfg => { }, typeof(InfrastructureAutoMapperProfile).Assembly);
        services.AddScoped<IProductAndDiscountRepository, ProductAndDiscountRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();

        return services;
    }

    public static IServiceCollection AddMongoInfrastructureServices(this IServiceCollection services, bool isDevelopment)
    {
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
            return client.GetDatabase(settings.DatabaseName);
        });

        return services;
    }

    public static IServiceCollection AddElasticInfrastructureServices(this IServiceCollection services, bool isDevelopment)
    {
        services.AddSingleton(sp =>
        {
            var elasticSettings = sp.GetRequiredService<IOptions<ElasticSearchSettings>>().Value;

            // ADD THIS DEBUG LOGGING
            Console.WriteLine($"ENV: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
            Console.WriteLine("=== ELASTICSEARCH CONFIGURATION ===");
            Console.WriteLine($"Akshay NodeUrl: {elasticSettings.NodeUrl}");
            Console.WriteLine($"Akshay Username: {elasticSettings.Username}");
            Console.WriteLine($"Akshay Password: {elasticSettings.Password}");
            Console.WriteLine($"Akshay IndexName: {elasticSettings.IndexName}");
            Console.WriteLine($"Akshay IndexAlias: {elasticSettings.IndexAlias}");
            Console.WriteLine("About to read CloudId");
            try
            {
                var cloudId = elasticSettings.CloudId;
                Console.WriteLine($"CloudId VALUE: {cloudId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CloudId GETTER THREW");
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("===================================");

            ElasticsearchClientSettings elasticConnectionSettings;
            var authentication = new BasicAuthentication(
                elasticSettings.Username,
                elasticSettings.Password
            );
            if (!string.IsNullOrWhiteSpace(elasticSettings.CloudId))
            {
                // Elastic Cloud
                elasticConnectionSettings = new ElasticsearchClientSettings(elasticSettings.CloudId, authentication);
            }
            else
            {
                Uri uri = new Uri(elasticSettings.NodeUrl);
                // Local Docker / self-hosted ES
                elasticConnectionSettings = new ElasticsearchClientSettings(uri)
                    .Authentication(
                        authentication
                        )
                    .ServerCertificateValidationCallback(CertificateValidations.AllowAll); // for self-signed certs;

            }
            elasticConnectionSettings
                .DefaultMappingFor<ProductEntity>(m => m
                   .IdProperty(p => p.Id))
               // Required additions for ES 8.x stability:
               .RequestTimeout(TimeSpan.FromSeconds(60))      // ES operations can be slow at startup
               .PingTimeout(TimeSpan.FromSeconds(30));         // avoid premature ping failures

            #if DEBUG
                elasticConnectionSettings.EnableDebugMode();       // replaces DisableDirectStreaming for debugging
            #endif

            return new ElasticsearchClient(elasticConnectionSettings);
        });

        return services;
    }
}