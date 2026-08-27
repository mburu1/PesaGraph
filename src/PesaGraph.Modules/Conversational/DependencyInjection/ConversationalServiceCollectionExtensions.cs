using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Conversational.DependencyInjection;

public static class ConversationalServiceCollectionExtensions
{
    public static IServiceCollection AddConversationalModule(this IServiceCollection services)
    {
        return services;
    }
}
