using Microsoft.Extensions.DependencyInjection;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Application.Services;
using MOTORFISCALSAPB1.Application.UseCases;

namespace MOTORFISCALSAPB1.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IFiscalSignatureService, FiscalSignatureService>();
        services.AddSingleton<ITaxCodeCodeGenerator, TaxCodeCodeGenerator>();
        services.AddSingleton<IDifalCalculator, DifalCalculator>();

        services.AddScoped<IFiscalRuleResolver, FiscalRuleResolver>();
        services.AddScoped<IFiscalContextBuilder, FiscalContextBuilder>();
        services.AddScoped<ITaxCodeEnsurer, TaxCodeEnsurer>();
        services.AddScoped<IFiscalEngine, FiscalEngine>();

        services.AddScoped<IResolveFiscalUseCase, ResolveFiscalUseCase>();
        services.AddScoped<IManageRuleUseCase, ManageRuleUseCase>();
        services.AddScoped<IInstallStructureUseCase, InstallStructureUseCase>();
        return services;
    }
}
