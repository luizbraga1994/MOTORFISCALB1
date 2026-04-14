using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOTORFISCALSAPB1.Application;
using MOTORFISCALSAPB1.Infrastructure;
using MOTORFISCALSAPB1.Integration.SapB1;
using MOTORFISCALSAPB1.Worker.Workers;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, _, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext())
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(ctx.Configuration);
        services.AddSapB1Integration(ctx.Configuration);
        services.AddHostedService<RuleCacheRefreshWorker>();
    })
    .Build();

await host.RunAsync();
