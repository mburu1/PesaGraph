using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Providers.Airtel;
using PesaGraph.Providers.Daraja;
using PesaGraph.Providers.Options;
using PesaGraph.Providers.Sms;
using PesaGraph.Providers.WhatsApp;

namespace PesaGraph.Providers.DependencyInjection;

public static class ProvidersServiceCollectionExtensions
{
    public static IServiceCollection AddProviderAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind and validate options on start
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

        // 2. Register HttpClients with Resilience
        services.AddHttpClient<IDarajaClient, DarajaClient>();
        services.AddHttpClient<IAirtelMoneyClient, AirtelMoneyClient>();
        services.AddHttpClient<IWhatsAppClient, WhatsAppClient>();
        services.AddHttpClient<ISmsClient, SmsClient>();

        return services;
    }
}
