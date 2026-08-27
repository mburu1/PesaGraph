using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Ingestion.Normalisers;
using PesaGraph.Ingestion.Repositories;
using PesaGraph.Ingestion.Services;

namespace PesaGraph.Ingestion.DependencyInjection;

public static class IngestionServiceCollectionExtensions
{
    public static IServiceCollection AddIngestionModule(this IServiceCollection services)
    {
        services.AddSingleton<IRawWebhookRepository, InMemoryRawWebhookRepository>();
        services.AddSingleton<IPayloadNormaliser, DarajaC2BNormaliser>();
        services.AddSingleton<IPayloadNormaliser, AirtelMoneyNormaliser>();
        services.AddScoped<IIngestionService, IngestionService>();

        return services;
    }
}
