using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Notifications.Services;
using PesaGraph.Providers.Sms;
using PesaGraph.Providers.WhatsApp;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using Xunit;

namespace PesaGraph.UnitTests.Notifications;

public class NotificationDispatcherTests
{
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly ISmsClient _smsClient;
    private readonly NotificationDispatcher _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public NotificationDispatcherTests()
    {
        _whatsAppClient = Substitute.For<IWhatsAppClient>();
        _smsClient = Substitute.For<ISmsClient>();
        _sut = new NotificationDispatcher(_whatsAppClient, _smsClient);
    }

    [Fact]
    public async Task SendNotificationAsync_WithWhatsAppPreferred_SendsViaWhatsApp()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Success("wamid.123"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.Received(1).SendTextMessageAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>());
        await _smsClient.DidNotReceive().SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendNotificationAsync_WithWhatsAppPreferredButFails_FallsBackToSms()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("WhatsApp.SendFailed", "WhatsApp service unavailable")));
        
        _smsClient.SendSmsAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Success("sms-msg-123"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.Received(1).SendTextMessageAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>());
        await _smsClient.Received(1).SendSmsAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendNotificationAsync_WithWhatsAppDisabled_SendsViaSms()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Hello, this is a test notification",
            PreferWhatsApp: false);

        _smsClient.SendSmsAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Success("sms-msg-456"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _smsClient.Received(1).SendSmsAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyPhone_ReturnsValidationError()
    {
        var message = new NotificationMessage(
            TenantId,
            "",
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        var result = await _sut.SendNotificationAsync(message);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NoRecipient");
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _smsClient.DidNotReceive().SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendNotificationAsync_WithNullPhone_ReturnsValidationError()
    {
        var message = new NotificationMessage(
            TenantId,
            null!,
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        var result = await _sut.SendNotificationAsync(message);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NoRecipient");
    }

    [Fact]
    public async Task SendNotificationAsync_WithWhitespacePhone_ReturnsValidationError()
    {
        var message = new NotificationMessage(
            TenantId,
            "   ",
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        var result = await _sut.SendNotificationAsync(message);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NoRecipient");
    }

    [Fact]
    public async Task SendNotificationAsync_WithBothChannelsFailing_ReturnsSmsError()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Hello, this is a test notification",
            PreferWhatsApp: true);

        var whatsAppError = Error.Failure("WhatsApp.SendFailed", "WhatsApp unavailable");
        var smsError = Error.Failure("Sms.SendFailed", "SMS service error");

        _whatsAppClient.SendTextMessageAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(whatsAppError));
        
        _smsClient.SendSmsAsync("254712345678", "Hello, this is a test notification", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(smsError));

        var result = await _sut.SendNotificationAsync(message);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendNotificationAsync_WithLongMessage_SendsSuccessfully()
    {
        var longMessage = new string('a', 1000);
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            longMessage,
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", longMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Success("wamid.789"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotificationAsync_WithSpecialCharacters_SendsSuccessfully()
    {
        var specialMessage = "Hello! 👋 Test: [Alert] {Status} = Complete✅";
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            specialMessage,
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", specialMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Success("wamid.special"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotificationAsync_WithCancellationToken_PassesThroughToClients()
    {
        var cts = new CancellationTokenSource();
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Test message",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", "Test message", cts.Token)
            .Returns(Result.Success("wamid.abc"));

        var result = await _sut.SendNotificationAsync(message, cts.Token);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.Received(1).SendTextMessageAsync("254712345678", "Test message", cts.Token);
    }

    [Fact]
    public async Task SendNotificationAsync_PreferWhatsAppTrueButSmsSucceeds_ReturnsSuccess()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "Test message",
            PreferWhatsApp: true);

        _whatsAppClient.SendTextMessageAsync("254712345678", "Test message", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("WhatsApp.Unavailable", "Service down")));
        
        _smsClient.SendSmsAsync("254712345678", "Test message", Arg.Any<CancellationToken>())
            .Returns(Result.Success("sms-123"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotificationAsync_PreferWhatsAppFalseSkipsWhatsApp_OnlySendsToSms()
    {
        var message = new NotificationMessage(
            TenantId,
            "254712345678",
            "SMS only message",
            PreferWhatsApp: false);

        _smsClient.SendSmsAsync("254712345678", "SMS only message", Arg.Any<CancellationToken>())
            .Returns(Result.Success("sms-789"));

        var result = await _sut.SendNotificationAsync(message);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
