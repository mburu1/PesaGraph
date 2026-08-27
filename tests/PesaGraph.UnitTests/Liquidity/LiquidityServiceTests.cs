using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Services;
using PesaGraph.Liquidity.Services;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using Xunit;

namespace PesaGraph.UnitTests.Liquidity;

public class LiquidityServiceTests
{
    private readonly ILedgerService _ledgerService;
    private readonly LiquidityService _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public LiquidityServiceTests()
    {
        _ledgerService = Substitute.For<ILedgerService>();
        _sut = new LiquidityService(_ledgerService);
    }

    private static Account BuildAccount(string name, PaymentRail rail, string accNumber, decimal balance)
    {
        var acc = Account.Create(TenantId, name, AccountType.MpesaTill, rail, accNumber);
        if (balance > 0) acc.Credit(balance);
        return acc;
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenAccountsExist_ShouldReturnSummary()
    {
        var accounts = new List<Account>
        {
            BuildAccount("M-Pesa Till", PaymentRail.Mpesa, "1111", 100_000m),
            BuildAccount("Airtel Float", PaymentRail.AirtelMoney, "2222", 50_000m),
            BuildAccount("Bank KCB", PaymentRail.Bank, "3333", 200_000m),
        };

        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)accounts));

        var result = await _sut.GetFloatCockpitAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalLiquidFloat.Should().Be(350_000m);
        result.Value.MpesaFloat.Should().Be(100_000m);
        result.Value.AirtelFloat.Should().Be(50_000m);
        result.Value.BankFloat.Should().Be(200_000m);
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenNoAccounts_ShouldReturnZeroTotals()
    {
        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)new List<Account>()));

        var result = await _sut.GetFloatCockpitAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalLiquidFloat.Should().Be(0m);
        result.Value.ActiveAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenLedgerFails_ShouldPropagateError()
    {
        var error = Error.NotFound("Ledger.NotFound", "No accounts found.");
        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<Account>>(error));

        var result = await _sut.GetFloatCockpitAsync(TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenAccountBelowThreshold_ShouldGenerateWarningAlert()
    {
        var accounts = new List<Account>
        {
            BuildAccount("Low Till", PaymentRail.Mpesa, "9999", 10_000m),
        };

        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)accounts));

        var result = await _sut.GetFloatCockpitAsync(TenantId, lowFloatThreshold: 50_000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveAlerts.Should().ContainSingle();
        result.Value.ActiveAlerts[0].Severity.Should().Be(FloatAlertSeverity.Warning);
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenAccountAtOrBelowZero_ShouldGenerateCriticalAlert()
    {
        var accounts = new List<Account>
        {
            BuildAccount("Empty Till", PaymentRail.Mpesa, "0000", 0m),
        };

        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)accounts));

        var result = await _sut.GetFloatCockpitAsync(TenantId, lowFloatThreshold: 50_000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveAlerts.Should().ContainSingle();
        result.Value.ActiveAlerts[0].Severity.Should().Be(FloatAlertSeverity.Critical);
    }

    [Fact]
    public async Task GetFloatCockpitAsync_WhenAccountAboveThreshold_ShouldNotGenerateAlerts()
    {
        var accounts = new List<Account>
        {
            BuildAccount("Healthy Till", PaymentRail.Mpesa, "7777", 200_000m),
        };

        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)accounts));

        var result = await _sut.GetFloatCockpitAsync(TenantId, lowFloatThreshold: 50_000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFloatCockpitAsync_ShouldPopulateAccountSummaries()
    {
        var accounts = new List<Account>
        {
            BuildAccount("Till A", PaymentRail.Mpesa, "A001", 75_000m),
            BuildAccount("Till B", PaymentRail.AirtelMoney, "A002", 25_000m),
        };

        _ledgerService.GetAccountsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success((IReadOnlyList<Account>)accounts));

        var result = await _sut.GetFloatCockpitAsync(TenantId);

        result.Value.Accounts.Should().HaveCount(2);
    }
}
