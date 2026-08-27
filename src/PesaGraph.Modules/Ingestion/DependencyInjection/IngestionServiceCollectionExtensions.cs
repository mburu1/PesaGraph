using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Ingestion.DependencyInjection;

public static class IngestionServiceCollectionExtensions
{
    public static IServiceCollection AddIngestionModule(this IServiceCollection services)
    {
        return services;
    }
}
