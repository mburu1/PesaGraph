using System;
using System.Collections.Generic;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;

namespace PesaGraph.Liquidity.DTOs;

public record AccountFloatSummary(
    Guid AccountId,
    string AccountName,
    PaymentRail Rail,
    string AccountNumber,
    decimal CurrentBalance,
    string Currency);

public record FloatCockpitSummary(
    Guid TenantId,
    decimal TotalLiquidFloat,
    decimal MpesaFloat,
    decimal AirtelFloat,
    decimal BankFloat,
    decimal CashFloat,
    string Currency,
    IReadOnlyList<AccountFloatSummary> Accounts,
    IReadOnlyList<FloatAlertDto> ActiveAlerts,
    DateTimeOffset GeneratedAtUtc);

public enum FloatAlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public record FloatAlertDto(
    string AccountName,
    PaymentRail Rail,
    decimal CurrentBalance,
    decimal Threshold,
    FloatAlertSeverity Severity,
    string Message);
