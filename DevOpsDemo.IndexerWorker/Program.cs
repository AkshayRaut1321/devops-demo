using DevOpsDemo.IndexerWorker;
using DevOpsDemo.Infrastructure.Entities.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using DevOpsDemo.IndexerWorker.Services;
using DevOpsDemo.Infrastructure;
using Serilog;
using DevOpsDemo.IndexerWorker.Entities.Config;

var builder = Host.CreateApplicationBuilder(args);

// Load default + environment-specific JSON
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddInfrastructureServices();
builder.Services.AddElasticInfrastructureServices(builder.Environment.IsDevelopment());

// -------------------------------------------------------
// Serilog (console only for now)
// -------------------------------------------------------
builder.Services.AddSingleton(Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger());

// -------------------------------------------------------
// Load configuration
// -------------------------------------------------------
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbIndexer"));

builder.Services.Configure<ElasticSearchSettings>(
    builder.Configuration.GetSection("ElasticSearchIndexer"));

builder.Services.Configure<WorkerSettings>(
    builder.Configuration.GetSection("WorkerIndexer"));

// -------------------------------------------------------
// Mongo client factory
// -------------------------------------------------------
builder.Services.AddSingleton<MongoClientFactory>();
builder.Services.AddSingleton<ElasticClientFactory>();
// -------------------------------------------------------
// bootstrap hosted service
// -------------------------------------------------------
builder.Services.AddHostedService<ElasticBootstrapService>();
// -------------------------------------------------------
// Worker - Change Streams listener (we implement this later)
// -------------------------------------------------------
builder.Services.AddHostedService<ChangeStreamWorker>();
builder.Services.AddHostedService<FullReindexWorker>();

var host = builder.Build();
host.Run();
