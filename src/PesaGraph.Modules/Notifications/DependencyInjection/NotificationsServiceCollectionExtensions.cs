using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Notifications.DependencyInjection;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        return services;
    }
}
