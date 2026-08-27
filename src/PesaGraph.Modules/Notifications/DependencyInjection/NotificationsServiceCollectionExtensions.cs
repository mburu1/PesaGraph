using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Notifications.Services;

namespace PesaGraph.Notifications.DependencyInjection;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        return services;
    }
}
