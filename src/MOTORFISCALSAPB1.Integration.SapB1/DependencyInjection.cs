using System.Net.Http;
using System.Net.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Integration.SapB1.Options;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;
using MOTORFISCALSAPB1.Integration.SapB1.Structure;
using MOTORFISCALSAPB1.Integration.SapB1.TaxCodes;
using Polly;
using Polly.Extensions.Http;

namespace MOTORFISCALSAPB1.Integration.SapB1;

public static class DependencyInjection
{
    public static IServiceCollection AddSapB1Integration(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<ServiceLayerOptions>(cfg.GetSection(ServiceLayerOptions.SectionName));

        services.AddHttpClient<IServiceLayerClient, ServiceLayerClient>()
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<ServiceLayerOptions>>().Value;
                var handler = new HttpClientHandler
                {
                    UseCookies = false
                };
                if (opts.IgnoreSslErrors)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        (_, _, _, _) => true;
                }
                return handler;
            })
            .AddPolicyHandler((sp, _) =>
            {
                var opts = sp.GetRequiredService<IOptions<ServiceLayerOptions>>().Value;
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        opts.MaxRetryAttempts,
                        retry => TimeSpan.FromMilliseconds(300 * Math.Pow(2, retry)));
            });

        services.AddSingleton<IManifestValidator, ManifestValidator>();
        services.AddScoped<IUserTablesMdService, UserTablesMdService>();
        services.AddScoped<IUserFieldsMdService, UserFieldsMdService>();
        services.AddScoped<IUserObjectsMdService, UserObjectsMdService>();
        services.AddScoped<ISapStructureService, SapStructureService>();
        services.AddScoped<ISapTaxCodeService, SapTaxCodeService>();

        return services;
    }
}
