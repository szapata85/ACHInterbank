using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaTransactionCodeSemanticRule(
    NachaEntryDirection Direction,
    AccountTypeEnum AccountType,
    bool IsPrenotification,
    string TransactionCode);

public sealed class NachaTransactionCodeSemanticContract
{
    private readonly IReadOnlyDictionary<(NachaEntryDirection Direction, AccountTypeEnum AccountType, bool IsPrenotification), NachaTransactionCodeSemanticRule> _byTuple;
    private readonly IReadOnlyDictionary<string, NachaTransactionCodeSemanticRule> _byCode;

    public NachaTransactionCodeSemanticContract(IEnumerable<NachaTransactionCodeSemanticRule> rules)
    {
        var materialized = rules.ToArray();
        _byTuple = materialized.ToDictionary(rule => (rule.Direction, rule.AccountType, rule.IsPrenotification));
        _byCode = materialized.ToDictionary(rule => rule.TransactionCode, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<NachaTransactionCodeSemanticRule> Rules => _byTuple.Values.ToArray();

    public bool TryGetRule(
        NachaEntryDirection direction,
        AccountTypeEnum accountType,
        bool isPrenotification,
        out NachaTransactionCodeSemanticRule rule)
        => _byTuple.TryGetValue((direction, accountType, isPrenotification), out rule!);

    public bool TryGetRule(string transactionCode, out NachaTransactionCodeSemanticRule rule)
        => _byCode.TryGetValue(transactionCode.Trim(), out rule!);
}

public enum NachaTransactionCodeSemanticMetadataStatus
{
    NotPresent = 0,
    Resolved = 1,
    Invalid = 2
}

public sealed record NachaTransactionCodeSemanticMetadataResult(
    NachaTransactionCodeSemanticMetadataStatus Status,
    NachaTransactionCodeSemanticContract? Contract = null,
    string? ErrorCode = null,
    string? Error = null);

public static class NachaTransactionCodeSemanticMetadata
{
    public const string RuleCodePrefix = "TRANSACTION_CODE_";
    public const string RequiredRuleType = "CONDITIONAL";
    public const string RequiredScope = "ENTRY";

    private static readonly HashSet<string> SupportedCodes =
    [
        "22", "23", "27", "28",
        "32", "33", "37", "38",
        "52", "53", "55", "57"
    ];

    public static NachaTransactionCodeSemanticMetadataResult Resolve(IEnumerable<CfgProfileRecord> profileRecords)
    {
        var records = profileRecords.Where(record => record.IsEnabled).ToArray();
        var entryRecords = records
            .Where(record => string.Equals(record.RecordCode?.Code, "6", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (entryRecords.Length != 1)
        {
            return Invalid("INVALID_TRANSACTION_CODE_RECORD", "El perfil requiere exactamente un record T6 habilitado.");
        }

        var entryRecord = entryRecords[0];
        if (!entryRecord.SemanticRuleSetId.HasValue)
        {
            return new(NachaTransactionCodeSemanticMetadataStatus.NotPresent);
        }
        if (entryRecord.SemanticRuleSet is null)
        {
            return Invalid("BAD_TRANSACTION_CODE_RULE_SET_REFERENCE", "SemanticRuleSetId de T6 referencia un conjunto inexistente.");
        }

        return Resolve(
            entryRecord.SemanticRuleSet.Scope,
            entryRecord.SemanticRuleSet.Rules.Select(rule => new PersistedRule(
                rule.RuleType?.Code,
                rule.RuleCode,
                rule.RuleConfigJson)));
    }

    public static NachaTransactionCodeSemanticMetadataResult Resolve(IEnumerable<NachaPublicationSnapshotRecord> profileRecords)
    {
        var records = profileRecords.Where(record => record.IsEnabled).ToArray();
        var entryRecords = records
            .Where(record => string.Equals(record.RecordCode, "6", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (entryRecords.Length != 1)
        {
            return Invalid("INVALID_TRANSACTION_CODE_RECORD", "El snapshot requiere exactamente un record T6 habilitado.");
        }

        var entryRecord = entryRecords[0];
        if (!entryRecord.SemanticRuleSetId.HasValue)
        {
            return new(NachaTransactionCodeSemanticMetadataStatus.NotPresent);
        }
        if (entryRecord.SemanticRuleSet is null)
        {
            return Invalid("BAD_TRANSACTION_CODE_RULE_SET_REFERENCE", "SemanticRuleSetId de T6 no fue preservado por valor.");
        }

        return Resolve(
            entryRecord.SemanticRuleSet.Scope,
            entryRecord.SemanticRuleSet.Rules.Select(rule => new PersistedRule(
                rule.RuleTypeCode,
                rule.RuleCode,
                rule.RuleConfiguration?.GetRawText())));
    }

    private static NachaTransactionCodeSemanticMetadataResult Resolve(
        string? scope,
        IEnumerable<PersistedRule> persistedRules)
    {
        if (!string.Equals(scope?.Trim(), RequiredScope, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("INVALID_TRANSACTION_CODE_RULE_SET_SCOPE", $"El conjunto semántico T6 debe usar Scope={RequiredScope}.");
        }

        var tuples = new Dictionary<(NachaEntryDirection, AccountTypeEnum, bool), NachaTransactionCodeSemanticRule>();
        var codes = new Dictionary<string, NachaTransactionCodeSemanticRule>(StringComparer.Ordinal);
        foreach (var persistedRule in persistedRules)
        {
            if (string.IsNullOrWhiteSpace(persistedRule.RuleCode)
                || !persistedRule.RuleCode.StartsWith(RuleCodePrefix, StringComparison.Ordinal)
                || !string.Equals(persistedRule.RuleTypeCode, RequiredRuleType, StringComparison.OrdinalIgnoreCase))
            {
                return Invalid("UNSUPPORTED_TRANSACTION_CODE_RULE", $"Regla semántica T6 no soportada: {persistedRule.RuleCode}.");
            }

            if (!TryParseRule(persistedRule.ConfigurationJson, out var rule, out var error))
            {
                return Invalid("MALFORMED_TRANSACTION_CODE_TUPLE", error);
            }

            var expectedRuleCode = RuleCodePrefix + rule!.TransactionCode;
            if (!string.Equals(persistedRule.RuleCode, expectedRuleCode, StringComparison.Ordinal))
            {
                return Invalid("TRANSACTION_CODE_RULE_IDENTITY_MISMATCH", $"RuleCode y TransactionCode no son coherentes para {rule.TransactionCode}.");
            }

            var tuple = (rule.Direction, rule.AccountType, rule.IsPrenotification);
            if (tuples.ContainsKey(tuple))
            {
                return Invalid("DUPLICATE_TRANSACTION_CODE_TUPLE", $"La tupla {Format(rule)} está duplicada.");
            }
            if (codes.TryGetValue(rule.TransactionCode, out var existing))
            {
                return Invalid(
                    existing == rule ? "DUPLICATE_TRANSACTION_CODE" : "AMBIGUOUS_TRANSACTION_CODE",
                    $"TransactionCode {rule.TransactionCode} tiene más de un significado dentro de la autoridad.");
            }

            tuples.Add(tuple, rule);
            codes.Add(rule.TransactionCode, rule);
        }

        var requiredTuples = Enum.GetValues<NachaEntryDirection>()
            .SelectMany(direction => Enum.GetValues<AccountTypeEnum>()
                .SelectMany(accountType => new[] { false, true }
                    .Select(isPrenotification => (direction, accountType, isPrenotification))))
            .ToArray();
        var missing = requiredTuples.Where(tuple => !tuples.ContainsKey(tuple)).ToArray();
        if (missing.Length > 0 || tuples.Count != requiredTuples.Length)
        {
            return Invalid("INCOMPLETE_TRANSACTION_CODE_MATRIX", "La autoridad T6 debe declarar exactamente las 12 tuplas ordinarias soportadas.");
        }

        return new(
            NachaTransactionCodeSemanticMetadataStatus.Resolved,
            new NachaTransactionCodeSemanticContract(tuples.Values));
    }

    private static bool TryParseRule(
        string? json,
        out NachaTransactionCodeSemanticRule? rule,
        out string error)
    {
        rule = null;
        error = "La configuración semántica T6 es inválida.";
        try
        {
            using var document = JsonDocument.Parse(json ?? string.Empty);
            var root = document.RootElement;
            var transactionCode = root.TryGetProperty("transactionCode", out var codeElement)
                ? codeElement.GetString()?.Trim()
                : null;
            var directionValue = root.TryGetProperty("direction", out var directionElement)
                ? directionElement.GetString()?.Trim()
                : null;
            var accountTypeValue = root.TryGetProperty("accountType", out var accountTypeElement)
                ? accountTypeElement.GetString()?.Trim()
                : null;
            if (string.IsNullOrWhiteSpace(transactionCode) || !SupportedCodes.Contains(transactionCode))
            {
                error = $"TransactionCode no soportado: {transactionCode ?? "<null>"}.";
                return false;
            }
            if (!Enum.TryParse<NachaEntryDirection>(directionValue, true, out var direction)
                || !Enum.IsDefined(direction))
            {
                error = $"Dirección T6 no soportada: {directionValue ?? "<null>"}.";
                return false;
            }
            if (!Enum.TryParse<AccountTypeEnum>(accountTypeValue, true, out var accountType)
                || !Enum.IsDefined(accountType))
            {
                error = $"AccountType T6 no soportado: {accountTypeValue ?? "<null>"}.";
                return false;
            }
            if (!root.TryGetProperty("isPrenotification", out var prenoteElement)
                || prenoteElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                error = "La tupla T6 requiere isPrenotification booleano.";
                return false;
            }

            rule = new(direction, accountType, prenoteElement.GetBoolean(), transactionCode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string Format(NachaTransactionCodeSemanticRule rule)
        => $"{rule.Direction}/{rule.AccountType}/{(rule.IsPrenotification ? "Prenotification" : "Monetary")}";

    private static NachaTransactionCodeSemanticMetadataResult Invalid(string errorCode, string error)
        => new(NachaTransactionCodeSemanticMetadataStatus.Invalid, ErrorCode: errorCode, Error: error);

    private sealed record PersistedRule(string? RuleTypeCode, string RuleCode, string? ConfigurationJson);
}
