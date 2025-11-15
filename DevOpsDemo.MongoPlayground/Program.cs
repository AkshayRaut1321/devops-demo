// See https://aka.ms/new-console-template for more information
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

Console.WriteLine("Hello, World!");

var mongoSettings = MongoClientSettings.FromConnectionString("mongodb://localhost:8001?directConnection=true");

mongoSettings.ClusterConfigurator = cb =>
{
    cb.Subscribe<CommandStartedEvent>(e =>
    {
        Console.WriteLine($"Mongo Command Started: {e.CommandName} - {e.Command.ToJson()}");
    });
};

var client = new MongoClient(mongoSettings);
var db = client.GetDatabase("Playground");

// Pick the demo you want to run
await Playground.BasicCrud.RunAsync(db);
// await Playground.PaginationPlayground.RunAsync(db);