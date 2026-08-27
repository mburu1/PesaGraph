using System;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Providers.Sms;
using PesaGraph.Providers.WhatsApp;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Notifications.Services;

public record NotificationMessage(
    Guid TenantId,
    string RecipientPhone,
    string Message,
    bool PreferWhatsApp = true);

public interface INotificationDispatcher
{
    Task<Result> SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
}

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly ISmsClient _smsClient;

    public NotificationDispatcher(IWhatsAppClient whatsAppClient, ISmsClient smsClient)
    {
        _whatsAppClient = whatsAppClient;
        _smsClient = smsClient;
    }

    public async Task<Result> SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.RecipientPhone))
        {
            return Result.Failure(Error.Validation("Notification.NoRecipient", "Recipient phone number is required."));
        }

        // Try WhatsApp first if preferred, fallback to SMS
        if (notification.PreferWhatsApp)
        {
            var waResult = await _whatsAppClient.SendTextMessageAsync(notification.RecipientPhone, notification.Message, cancellationToken);
            if (waResult.IsSuccess) return Result.Success();
        }

        var smsResult = await _smsClient.SendSmsAsync(notification.RecipientPhone, notification.Message, cancellationToken);
        return smsResult.IsSuccess
            ? Result.Success()
            : Result.Failure(smsResult.Error);
    }
}
