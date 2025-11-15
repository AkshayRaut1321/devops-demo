using DevOpsDemo.IndexerWorker;
using DevOpsDemo.IndexerWorker.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using DevOpsDemo.IndexerWorker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

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
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<ElasticSearchSettings>(
    builder.Configuration.GetSection("Elasticsearch"));

builder.Services.Configure<WorkerSettings>(
    builder.Configuration.GetSection("Worker"));

// -------------------------------------------------------
// Mongo client factory
// -------------------------------------------------------
builder.Services.AddSingleton<MongoClientFactory>();
builder.Services.AddSingleton<ElasticClientFactory>();

// -------------------------------------------------------
// Worker - Change Streams listener (we implement this later)
// -------------------------------------------------------
builder.Services.AddHostedService<ChangeStreamWorker>();

var host = builder.Build();
host.Run();
