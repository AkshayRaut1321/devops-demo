using DevOpsDemo.IndexerWorker;
using DevOpsDemo.Infrastructure.Entities.Config;
using DevOpsDemo.IndexerWorker.Infrastructure;
using DevOpsDemo.IndexerWorker.Services;
using DevOpsDemo.Infrastructure;
// using Serilog;
using DevOpsDemo.IndexerWorker.Entities.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

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
//Removing it temporarily due to conflicts with ILogger implementation as I cannot see logs of ElasticBootstrapService as per ChatGPT suggestion
// builder.Services.AddSingleton(Log.Logger = new LoggerConfiguration()
//     .WriteTo.Console()
//     .CreateLogger());

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
// Readiness state
builder.Services.AddSingleton<IndexerReadiness>();
// -------------------------------------------------------
// bootstrap hosted service
// -------------------------------------------------------
builder.Services.AddHostedService<ElasticBootstrapService>();
// -------------------------------------------------------
// Worker - Change Streams listener (we implement this later)
// -------------------------------------------------------
builder.Services.AddHostedService<ChangeStreamWorker>();
builder.Services.AddHostedService<FullReindexWorker>();

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

app.MapGet("/health/ready", (IndexerReadiness readiness) =>
{
    return readiness.IsReady ? Results.Ok("Indexer ready") : Results.StatusCode(503);
});

// Single lifecycle owner
app.Run();