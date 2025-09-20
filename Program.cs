using OrderProvision.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddProductHandlers();
    })
    .Build();

await host.RunAsync();