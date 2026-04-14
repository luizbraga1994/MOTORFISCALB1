using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Infrastructure.Caching;
using MOTORFISCALSAPB1.Infrastructure.Locking;
using MOTORFISCALSAPB1.Infrastructure.Options;
using MOTORFISCALSAPB1.Infrastructure.Persistence;
using MOTORFISCALSAPB1.Infrastructure.Repositories;
using MOTORFISCALSAPB1.Infrastructure.Sap;

namespace MOTORFISCALSAPB1.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<HanaOptions>(cfg.GetSection(HanaOptions.SectionName));
        services.AddSingleton<IHanaConnectionFactory, HanaConnectionFactory>();

        services.AddMemoryCache();
        services.AddSingleton<IFiscalRuleCache, FiscalRuleCache>();

        services.AddScoped<ISapReadPort, SapReadPort>();
        services.AddScoped<IFiscalRuleRepository, FiscalRuleRepository>();
        services.AddScoped<ITaxCodeMappingRepository, TaxCodeMappingRepository>();
        services.AddScoped<IFiscalAuditRepository, FiscalAuditRepository>();
        services.AddScoped<IDistributedFiscalLock, HanaDistributedFiscalLock>();

        return services;
    }
}
