using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public static class NachaTransactionCodeTaxonomy
{
    private static readonly IReadOnlyDictionary<(TransactionTypeEnum Type, AccountTypeEnum Account, bool IsPrenotification), string> Codes
        = new Dictionary<(TransactionTypeEnum, AccountTypeEnum, bool), string>
        {
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false), "22" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true), "23" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, true), "28" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false), "32" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, true), "33" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, true), "38" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false), "52" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, true), "53" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false), "55" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true), "57" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.Checking, true), "23" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.Savings, true), "33" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.ElectronicDeposits, true), "53" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.ElectronicDeposits, false), "55" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.ElectronicDeposits, false), "55" }
        };

    public static bool TryResolve(
        TransactionTypeEnum type,
        AccountTypeEnum account,
        bool isPrenotification,
        out string code)
        => Codes.TryGetValue((type, account, isPrenotification), out code!);

    public static TransactionTypeEnum? ResolvePrenotificationDirection(string? code)
    {
        var normalized = code?.Trim();
        if (Codes.Any(item => item.Key.IsPrenotification
                && item.Key.Type == TransactionTypeEnum.Credit
                && item.Value == normalized))
        {
            return TransactionTypeEnum.Credit;
        }

        if (Codes.Any(item => item.Key.IsPrenotification
                && item.Key.Type == TransactionTypeEnum.Debit
                && item.Value == normalized))
        {
            return TransactionTypeEnum.Debit;
        }

        return null;
    }

    public static string? ResolvePrenotificationCode(string? monetaryCode)
    {
        var normalized = monetaryCode?.Trim();
        var monetary = Codes.FirstOrDefault(item => !item.Key.IsPrenotification
            && item.Key.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Debit
            && item.Value == normalized);
        if (string.IsNullOrWhiteSpace(monetary.Value)
            || monetary.Key.Type is not (TransactionTypeEnum.Credit or TransactionTypeEnum.Debit))
        {
            return null;
        }

        return Codes.GetValueOrDefault((monetary.Key.Type, monetary.Key.Account, true));
    }
}
