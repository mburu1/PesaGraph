using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Conversational.Services;
using PesaGraph.Liquidity.Services;
using PesaGraph.Providers.WhatsApp;
using PesaGraph.Reconciliation.Services;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using Xunit;

namespace PesaGraph.UnitTests.Conversational;

public class ConversationalCommandServiceTests
{
    private readonly IWhatsAppClient _whatsAppClient = Substitute.For<IWhatsAppClient>();
    private readonly ConversationalCommandService _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ConversationalCommandServiceTests()
    {
        _sut = new ConversationalCommandService(
            Substitute.For<ILiquidityService>(),
            Substitute.For<IReconciliationService>(),
            _whatsAppClient);
    }

    [Fact]
    public async Task HandleCommandAsync_HelpOverSms_ReturnsHelpWithoutSendingWhatsAppMessage()
    {
        var result = await _sut.HandleCommandAsync(new InboundCommand(TenantId, "+254700000000", "help", "SMS"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Available Commands");
        await _whatsAppClient.DidNotReceive().SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCommandAsync_WhenWhatsAppDeliveryFails_ReturnsDeliveryFailure()
    {
        var deliveryError = Error.Failure("WhatsApp.SendFailed", "Provider unavailable");
        _whatsAppClient.SendTextMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(deliveryError));

        var result = await _sut.HandleCommandAsync(new InboundCommand(TenantId, "+254700000000", "help"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(deliveryError);
    }

    [Fact]
    public async Task HandleCommandAsync_WithoutTenant_ReturnsValidationFailure()
    {
        var result = await _sut.HandleCommandAsync(new InboundCommand(Guid.Empty, "+254700000000", "help", "SMS"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversational.TenantRequired");
    }
}
