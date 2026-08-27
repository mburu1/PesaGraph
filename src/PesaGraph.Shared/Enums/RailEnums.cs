namespace PesaGraph.Shared.Enums;

public enum PaymentRail
{
    Mpesa = 1,
    AirtelMoney = 2,
    Bank = 3,
    CashFloat = 4,
    Other = 99
}

public enum TransactionType
{
    CustomerToBusiness = 1, // C2B (Till / Paybill)
    BusinessToCustomer = 2, // B2C (Disbursement)
    BusinessToBusiness = 3, // B2B
    BankTransfer = 4,       // Bank deposit / withdrawal / EFT / RTGS / Pesalink
    InternalTransfer = 5,   // Float rebalancing between accounts
    Fee = 6,                // Rail fee / levy
    Reversal = 7            // Transaction reversal
}

public enum MatchStatus
{
    Unmatched = 0,
    Matched = 1,
    PartiallyMatched = 2,
    ManuallyResolved = 3,
    Ignored = 4
}
