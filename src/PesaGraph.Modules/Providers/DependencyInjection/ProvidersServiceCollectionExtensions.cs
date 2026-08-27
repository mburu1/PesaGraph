using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Providers.Options;

namespace PesaGraph.Providers.DependencyInjection;

public static class ProvidersServiceCollectionExtensions
{
    public static IServiceCollection AddProviderAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DarajaOptions>()
            .Bind(configuration.GetSection(DarajaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AirtelMoneyOptions>()
            .Bind(configuration.GetSection(AirtelMoneyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<WhatsAppOptions>()
            .Bind(configuration.GetSection(WhatsAppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmsOptions>()
            .Bind(configuration.GetSection(SmsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
