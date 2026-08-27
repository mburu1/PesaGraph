using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Ledger.Services;

public record PostTransactionRequest(
    Guid TenantId,
    string AccountNumber,
    Guid TransactionId,
    string ExternalReference,
    decimal Amount,
    decimal FeeAmount,
    PaymentRail Rail,
    TransactionType Type,
    string CounterpartyInfo,
    string Description,
    string Currency = "KES");

public interface ILedgerService
{
    Task<Result<Account>> CreateAccountAsync(Guid tenantId, string name, AccountType type, PaymentRail rail, string accountNumber, string currency = "KES", CancellationToken cancellationToken = default);
    Task<Result<LedgerEntry>> PostTransactionAsync(PostTransactionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Account>>> GetAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LedgerEntry>>> GetAccountEntriesAsync(Guid accountId, int limit = 100, CancellationToken cancellationToken = default);
}

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepository;

    public LedgerService(ILedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<Result<Account>> CreateAccountAsync(Guid tenantId, string name, AccountType type, PaymentRail rail, string accountNumber, string currency = "KES", CancellationToken cancellationToken = default)
    {
        var existing = await _ledgerRepository.GetAccountByNumberAsync(tenantId, accountNumber, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<Account>(Error.Conflict("Account.Exists", $"Account with number '{accountNumber}' already exists for this tenant."));
        }

        var account = Account.Create(tenantId, name, type, rail, accountNumber, currency);
        await _ledgerRepository.AddAccountAsync(account, cancellationToken);

        return Result.Success(account);
    }

    public async Task<Result<LedgerEntry>> PostTransactionAsync(PostTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _ledgerRepository.GetAccountByNumberAsync(request.TenantId, request.AccountNumber, cancellationToken);
        if (account == null)
        {
            // Auto-create standard rail account if not exists
            var type = request.Rail switch
            {
                PaymentRail.Mpesa => AccountType.MpesaPaybill,
                PaymentRail.AirtelMoney => AccountType.AirtelMoney,
                PaymentRail.Bank => AccountType.BankAccount,
                _ => AccountType.CashFloat
            };

            account = Account.Create(request.TenantId, $"{request.Rail} Account", type, request.Rail, request.AccountNumber, request.Currency);
            await _ledgerRepository.AddAccountAsync(account, cancellationToken);
        }

        // Determine credit/debit direction
        var isCredit = request.Type is TransactionType.CustomerToBusiness or TransactionType.BankTransfer;
        var direction = isCredit ? EntryDirection.Credit : EntryDirection.Debit;

        if (isCredit)
        {
            account.Credit(request.Amount - request.FeeAmount);
        }
        else
        {
            account.Debit(request.Amount + request.FeeAmount);
        }

        await _ledgerRepository.UpdateAccountAsync(account, cancellationToken);

        var entry = new LedgerEntry(
            Guid.NewGuid(),
            request.TenantId,
            account.Id,
            request.TransactionId,
            request.ExternalReference,
            direction,
            new Money(request.Amount, request.Currency),
            new Money(request.FeeAmount, request.Currency),
            account.CurrentBalance,
            request.CounterpartyInfo,
            request.Description);

        await _ledgerRepository.AddEntryAsync(entry, cancellationToken);

        return Result.Success(entry);
    }

    public async Task<Result<IReadOnlyList<Account>>> GetAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var accounts = await _ledgerRepository.ListAccountsByTenantAsync(tenantId, cancellationToken);
        return Result.Success(accounts);
    }

    public async Task<Result<IReadOnlyList<LedgerEntry>>> GetAccountEntriesAsync(Guid accountId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var entries = await _ledgerRepository.ListEntriesByAccountAsync(accountId, limit, cancellationToken);
        return Result.Success(entries);
    }
}
