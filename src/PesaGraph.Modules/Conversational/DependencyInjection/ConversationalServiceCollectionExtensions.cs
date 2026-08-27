using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Conversational.Services;

namespace PesaGraph.Conversational.DependencyInjection;

public static class ConversationalServiceCollectionExtensions
{
    public static IServiceCollection AddConversationalModule(this IServiceCollection services)
    {
        services.AddScoped<IConversationalCommandService, ConversationalCommandService>();
        return services;
    }
}
