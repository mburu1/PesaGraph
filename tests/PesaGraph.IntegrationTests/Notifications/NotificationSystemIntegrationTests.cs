using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Notifications.Services;
using PesaGraph.Providers.Sms;
using PesaGraph.Providers.WhatsApp;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using Xunit;

namespace PesaGraph.IntegrationTests.Notifications;

public class NotificationSystemIntegrationTests : IAsyncLifetime
{
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly ISmsClient _smsClient;
    private readonly NotificationDispatcher _notificationDispatcher;
    private static readonly Guid TenantId = Guid.NewGuid();

    public NotificationSystemIntegrationTests()
    {
        _whatsAppClient = Substitute.For<IWhatsAppClient>();
        _smsClient = Substitute.For<ISmsClient>();
        _notificationDispatcher = new NotificationDispatcher(_whatsAppClient, _smsClient);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SendNotification_WithWhatsAppSuccess_ShouldNotFallbackToSms()
    {
        var notification = new NotificationMessage(
            TenantId,
            "254712345678",
            "Transaction alert: KES 5000 received",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Success("wamid.transaction-alert-001"));

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.Received(1).SendTextMessageAsync(
            "254712345678", "Transaction alert: KES 5000 received", Arg.Any<System.Threading.CancellationToken>());
        await _smsClient.DidNotReceive().SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task SendNotification_WithWhatsAppFailure_ShouldFallbackToSms()
    {
        var notification = new NotificationMessage(
            TenantId,
            "254712345678",
            "Account balance: KES 25,000",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("WhatsApp.ServiceUnavailable", "WhatsApp service is down")));

        _smsClient.SendSmsAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Success("sms-balance-check-001"));

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.Received(1).SendTextMessageAsync(
            "254712345678", "Account balance: KES 25,000", Arg.Any<System.Threading.CancellationToken>());
        await _smsClient.Received(1).SendSmsAsync(
            "254712345678", "Account balance: KES 25,000", Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task SendNotification_WithBothChannelsFailure_ShouldReturnError()
    {
        var notification = new NotificationMessage(
            TenantId,
            "254712345678",
            "Critical: Reconciliation failed",
            PreferWhatsApp: true);

        var whatsAppError = Error.Failure("WhatsApp.AuthFailed", "Invalid credentials");
        var smsError = Error.Failure("Sms.RateLimited", "Rate limit exceeded");

        _whatsAppClient.SendTextMessageAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Failure<string>(whatsAppError));

        _smsClient.SendSmsAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Failure<string>(smsError));

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.RateLimited");
    }

    [Fact]
    public async Task SendNotification_SmsOnly_ShouldSkipWhatsApp()
    {
        var notification = new NotificationMessage(
            TenantId,
            "254712345678",
            "OTP: 123456",
            PreferWhatsApp: false);

        _smsClient.SendSmsAsync(notification.RecipientPhone, notification.Message, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Success("sms-otp-001"));

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task SendNotification_InvalidPhoneNumber_ShouldReturnValidationError()
    {
        var notification = new NotificationMessage(
            TenantId,
            "",
            "Test message",
            PreferWhatsApp: true);

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NoRecipient");
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
        await _smsClient.DidNotReceive().SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task BulkNotifications_ShouldProcessAllWithFallback()
    {
        var notifications = new[]
        {
            new NotificationMessage(TenantId, "254712345678", "Message 1", PreferWhatsApp: true),
            new NotificationMessage(TenantId, "254723456789", "Message 2", PreferWhatsApp: true),
            new NotificationMessage(TenantId, "254734567890", "Message 3", PreferWhatsApp: true)
        };

        _whatsAppClient.SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(x => Result.Success($"wamid.{Guid.NewGuid()}"));

        var results = new System.Collections.Generic.List<Result>();
        foreach (var notification in notifications)
        {
            var result = await _notificationDispatcher.SendNotificationAsync(notification);
            results.Add(result);
        }

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        await _whatsAppClient.Received(3).SendTextMessageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task LongMessage_ShouldSendSuccessfully()
    {
        var longMessage = "Transaction processed: Customer Name: John Doe | Amount: KES 50,000 | Date: 2024-01-28 " +
                         "| Reason: Monthly Salary | Reference: SAL-2024-01 | Status: Completed | " +
                         "Account Balance: KES 125,000 | Processing Time: 2.5 seconds";

        var notification = new NotificationMessage(
            TenantId,
            "254712345678",
            longMessage,
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync(notification.RecipientPhone, longMessage, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result.Success("wamid.long-msg-001"));

        var result = await _notificationDispatcher.SendNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
    }
}
