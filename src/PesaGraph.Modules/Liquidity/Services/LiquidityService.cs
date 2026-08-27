using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ledger.Services;
using PesaGraph.Liquidity.DTOs;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Results;

namespace PesaGraph.Liquidity.Services;

public interface ILiquidityService
{
    Task<Result<FloatCockpitSummary>> GetFloatCockpitAsync(Guid tenantId, decimal lowFloatThreshold = 50000m, CancellationToken cancellationToken = default);
}

public class LiquidityService : ILiquidityService
{
    private readonly ILedgerService _ledgerService;

    public LiquidityService(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    public async Task<Result<FloatCockpitSummary>> GetFloatCockpitAsync(Guid tenantId, decimal lowFloatThreshold = 50000m, CancellationToken cancellationToken = default)
    {
        var accountsResult = await _ledgerService.GetAccountsAsync(tenantId, cancellationToken);
        if (accountsResult.IsFailure)
        {
            return Result.Failure<FloatCockpitSummary>(accountsResult.Error);
        }

        var accounts = accountsResult.Value;
        var accountSummaries = accounts.Select(a => new AccountFloatSummary(
            a.Id,
            a.Name,
            a.Rail,
            a.AccountNumber,
            a.CurrentBalance.Amount,
            a.CurrentBalance.Currency)).ToList();

        var mpesaTotal = accounts.Where(a => a.Rail == PaymentRail.Mpesa).Sum(a => a.CurrentBalance.Amount);
        var airtelTotal = accounts.Where(a => a.Rail == PaymentRail.AirtelMoney).Sum(a => a.CurrentBalance.Amount);
        var bankTotal = accounts.Where(a => a.Rail == PaymentRail.Bank).Sum(a => a.CurrentBalance.Amount);
        var cashTotal = accounts.Where(a => a.Rail == PaymentRail.CashFloat).Sum(a => a.CurrentBalance.Amount);
        var totalLiquidFloat = accounts.Sum(a => a.CurrentBalance.Amount);

        // Generate float alerts
        var alerts = new List<FloatAlertDto>();
        foreach (var acc in accounts)
        {
            if (acc.CurrentBalance.Amount < lowFloatThreshold)
            {
                var severity = acc.CurrentBalance.Amount <= 0 ? FloatAlertSeverity.Critical : FloatAlertSeverity.Warning;
                alerts.Add(new FloatAlertDto(
                    acc.Name,
                    acc.Rail,
                    acc.CurrentBalance.Amount,
                    lowFloatThreshold,
                    severity,
                    $"Low float alert: {acc.Name} ({acc.Rail}) balance is KES {acc.CurrentBalance.Amount:N2}, below threshold of KES {lowFloatThreshold:N2}."));
            }
        }

        var summary = new FloatCockpitSummary(
            tenantId,
            totalLiquidFloat,
            mpesaTotal,
            airtelTotal,
            bankTotal,
            cashTotal,
            "KES",
            accountSummaries,
            alerts,
            DateTimeOffset.UtcNow);

        return Result.Success(summary);
    }
}
