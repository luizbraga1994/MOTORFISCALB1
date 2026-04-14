using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOTORFISCALSAPB1.Application;
using MOTORFISCALSAPB1.Infrastructure;
using MOTORFISCALSAPB1.Integration.SapB1;
using MOTORFISCALSAPB1.Worker.Workers;
using Serilog;

// Integracao com Windows Service Control Manager (SCM):
// - responde a START/STOP/SHUTDOWN
// - escreve no Windows Event Log quando nao consegue logar em arquivo
// - no-op quando rodando em modo console (desenvolvimento / Linux / Docker)
// UseContentRoot fixa o diretorio do binario porque quando o SCM sobe o
// servico o CWD e' system32.
var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseWindowsService(o => o.ServiceName = "MOTORFISCALSAPB1.Worker")
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
