using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class IncomingProcTransaccionesE2eScenarioSetupService
    : IIncomingProcTransaccionesE2eScenarioSetupService
{
    public const string SetupAuthorizationVariable = "ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP";
    public const string ReceiverAccountVariable = "ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT";
    public const string ExpectedAmountVariable = "ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT";
    public const string TransactionExternalIdPrefix = "E2E-PTX-IN-";
    public const string BatchCompanyName = "ESCENARIO E2E PROC TRANSACCIONES";
    public const string SyntheticRecipientId = "E2EPTXANCHOR001";
    public const string IncomingTransactionCode = "32";
    public const string IncomingServiceClassCode = "220";
    public const string IncomingClearingHouseCode = "CENIT";

    private readonly AchDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public IncomingProcTransaccionesE2eScenarioSetupService(
        AchDbContext context,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    public Task<IncomingProcTransaccionesE2eScenarioResult> InspectAsync(
        IncomingProcTransaccionesE2eScenarioRequest request,
        CancellationToken ct = default)
        => ResolveAsync(request, allowCreate: false, ct);

    public Task<IncomingProcTransaccionesE2eScenarioResult> EnsureAsync(
        IncomingProcTransaccionesE2eScenarioRequest request,
        CancellationToken ct = default)
        => ResolveAsync(request, allowCreate: true, ct);

    private async Task<IncomingProcTransaccionesE2eScenarioResult> ResolveAsync(
        IncomingProcTransaccionesE2eScenarioRequest request,
        bool allowCreate,
        CancellationToken ct)
    {
        EnsureSafeEnvironment();
        EnsureSetupAuthorized();
        var (receiverAccount, authorizedAmount, amountInCents) = ReadAuthorizedValues();
        ValidateRequest(request);

        var cfaCandidates = await _context.FinancialInstitutions
            .Where(x => x.IsDefaultSource)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        if (cfaCandidates.Count != 1)
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_CFA_AMBIGUOUS: se requiere exactamente una FinancialInstitution IsDefaultSource=true y se encontraron {cfaCandidates.Count}.");
        }

        var cfa = cfaCandidates[0];
        if (cfa.Status != FinancialInstitutionStatus.Active)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_E2E_CFA_INACTIVE: la CFA canónica está inactiva; el setup no la modificará.");
        }

        var receivingDfi = ResolveDfi(cfa, "CFA");
        var externalResolution = await ResolveExternalInstitutionAsync(allowCreate, ct);
        var external = externalResolution.Institution;
        var externalDfi = ResolveDfi(external, "entidad externa sintética");
        var externalOriginRouting = externalDfi[..8];

        var achCycle = await ResolveCycleAsync(request.OperationalDate.Date, request.CycleNumber, ct);
        var scenarioKey = BuildScenarioKey(receiverAccount, amountInCents, cfa.Id, external.Id, achCycle.Id);

        var existingTransactions = await _context.AchTransactions
            .Include(x => x.AchBatch)
            .Where(x => x.TransactionExternalId == scenarioKey)
            .ToListAsync(ct);

        if (existingTransactions.Count > 1)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_TRANSACTION_AMBIGUOUS: existe más de una transacción sintética con la misma clave idempotente.");
        }

        var transaction = existingTransactions.SingleOrDefault();
        var createdTransaction = false;
        if (transaction is null)
        {
            if (!allowCreate)
            {
                throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_E2E_TRANSACTION_MISSING: la transacción receptora sintética no existe; ejecute el setup autorizado antes del preflight.");
            }

            var companyEntryDescription = await _context.CompanyEntryDescriptionCatalogs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Term == "NOMINAS")
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_E2E_ENTRY_DESCRIPTION_MISSING: no existe CompanyEntryDescription activo para el batch sintético.");

            var batch = await ResolveOrCreateSyntheticBatchAsync(
                achCycle,
                companyEntryDescription,
                scenarioKey,
                externalOriginRouting,
                authorizedAmount,
                ct);

            var traceNumber = BuildSyntheticTrace(externalOriginRouting, scenarioKey);
            transaction = new AchTransaction
            {
                Amount = authorizedAmount,
                TransactionExternalId = scenarioKey,
                Reference = scenarioKey,
                Type = TransactionTypeEnum.Credit,
                TransactionCode = IncomingTransactionCode,
                ServiceClassCode = IncomingServiceClassCode,
                CompanyEntryDescriptionId = companyEntryDescription.Id,
                CompanyName = BatchCompanyName,
                CompanyIdentification = scenarioKey,
                OriginatingDFI = externalDfi,
                ReceivingDFI = receivingDfi,
                TraceNumber = traceNumber,
                TraceSequenceNumber = ParseTraceSequence(traceNumber),
                EffectiveEntryDate = achCycle.ProcessingDate.Date,
                AddendaRecordIndicator = false,
                IsPrenotification = false,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = DateTime.UtcNow,
                RecipientIdNumber = SyntheticRecipientId,
                SourceAccountNumber = $"E2E-ORIGIN-{scenarioKey[^12..]}",
                DestinationAccountNumber = receiverAccount,
                SourceInstitutionId = external.Id,
                DestinationInstitutionId = cfa.Id,
                AchCycleId = achCycle.Id,
                AchBatch = batch
            };

            _context.AchTransactions.Add(transaction);
            await _context.SaveChangesAsync(ct);
            createdTransaction = true;
        }

        ValidateExistingTransaction(
            transaction,
            cfa,
            external,
            achCycle,
            receiverAccount,
            authorizedAmount,
            receivingDfi,
            externalDfi,
            scenarioKey);

        return new IncomingProcTransaccionesE2eScenarioResult
        {
            IsReady = true,
            SetupAuthorized = true,
            CreatedExternalInstitution = externalResolution.Created,
            CreatedTransaction = createdTransaction,
            CfaInstitutionId = cfa.Id,
            ExternalInstitutionId = external.Id,
            TransactionId = transaction.Id,
            AchCycleId = achCycle.Id,
            ReceivingDfi = receivingDfi,
            ExternalOriginRouting = externalOriginRouting,
            ReceiverAccountMasked = Mask(receiverAccount),
            AuthorizedAmount = authorizedAmount,
            TransactionExternalId = scenarioKey,
            Message = createdTransaction || externalResolution.Created
                ? "Escenario sintético Proc_Transacciones provisionado mediante EF Core."
                : "Escenario sintético Proc_Transacciones ya existía y fue validado sin duplicados."
        };
    }

    private void EnsureSafeEnvironment()
    {
        if (!_environment.IsDevelopment()
            && !_environment.IsEnvironment("Testing")
            && !_environment.IsEnvironment("UAT"))
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_ENVIRONMENT_BLOCKED: el setup sólo está permitido en Development, Testing o UAT.");
        }
    }

    private void EnsureSetupAuthorized()
    {
        if (!string.Equals(_configuration[SetupAuthorizationVariable], "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_SETUP_NOT_AUTHORIZED: defina {SetupAuthorizationVariable}=true para autorizar exclusivamente datos sintéticos; este flag no autoriza SOAP.");
        }
    }

    private (string Account, decimal Amount, long AmountInCents) ReadAuthorizedValues()
    {
        var account = (_configuration[ReceiverAccountVariable] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(account))
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_ACCOUNT_MISSING: {ReceiverAccountVariable} es obligatoria y no tiene fallback.");
        }

        if (account.Length > 17 || account.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_ACCOUNT_INVALID: la cuenta autorizada debe tener 1-17 caracteres ASCII alfanuméricos o guion.");
        }

        var rawAmount = (_configuration[ExpectedAmountVariable] ?? string.Empty).Trim();
        if (!Regex.IsMatch(rawAmount, @"^\d+(?:[\.,]\d{1,2})?$")
            || !decimal.TryParse(
                rawAmount.Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_AMOUNT_MISSING: {ExpectedAmountVariable} debe contener un decimal autorizado sin separadores de miles, usando punto o coma y mÃ¡ximo dos decimales.");
        }

        if (amount <= 0m)
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_AMOUNT_INVALID: TransactionCode={IncomingTransactionCode} requiere monto monetario mayor que cero.");
        }

        var scale = (decimal.GetBits(amount)[3] >> 16) & 0x7F;
        if (scale > 2)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_AMOUNT_SCALE_INVALID: el monto autorizado admite máximo dos decimales.");
        }

        var amountInCents = checked(decimal.ToInt64(amount * 100m));
        if (amountInCents.ToString(CultureInfo.InvariantCulture).Length > 18)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_AMOUNT_OVERFLOW: el monto no cabe en el campo NACHA-M de 18 dígitos.");
        }

        return (account, amount, amountInCents);
    }

    private static void ValidateRequest(IncomingProcTransaccionesE2eScenarioRequest request)
    {
        if (request.OperationalDate == default)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_E2E_OPERATIONAL_DATE_MISSING: la fecha del fixture es obligatoria.");
        }

        if (request.CycleNumber <= 0)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_E2E_CYCLE_INVALID: el número de ciclo debe ser positivo.");
        }
    }

    private async Task<(FinancialInstitution Institution, bool Created)> ResolveExternalInstitutionAsync(
        bool allowCreate,
        CancellationToken ct)
    {
        var candidates = await _context.FinancialInstitutions
            .Where(x => x.Name == FinancialInstitutionSeeder.SyntheticAchExternalName)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_EXTERNAL_AMBIGUOUS: existe más de una entidad Banco UAT Externo ACH.");
        }

        var external = candidates.SingleOrDefault();
        if (external is null)
        {
            if (!allowCreate)
            {
                throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_E2E_EXTERNAL_MISSING: no existe Banco UAT Externo ACH; ejecute el setup autorizado.");
            }

            external = new FinancialInstitution
            {
                Name = FinancialInstitutionSeeder.SyntheticAchExternalName,
                RoutingNumber = FinancialInstitutionSeeder.SyntheticAchExternalRouting,
                TransitCode = FinancialInstitutionSeeder.SyntheticAchExternalTransit,
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            external.CalculateCheckDigit();
            _context.FinancialInstitutions.Add(external);
            await _context.SaveChangesAsync(ct);
            return (external, true);
        }

        if (external.IsDefaultSource
            || external.Status != FinancialInstitutionStatus.Active
            || external.RoutingNumber != FinancialInstitutionSeeder.SyntheticAchExternalRouting
            || external.TransitCode != FinancialInstitutionSeeder.SyntheticAchExternalTransit)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_EXTERNAL_NOT_SYNTHETIC: la entidad con nombre UAT no conserva la marca/routing sintéticos aprobados y no será modificada.");
        }

        return (external, false);
    }

    private async Task<AchCycle> ResolveCycleAsync(DateTime operationalDate, int cycleNumber, CancellationToken ct)
    {
        var cycles = await _context.AchCycles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Where(x => x.ProcessingDate.Date == operationalDate.Date
                        && x.ClearingHouse != null
                        && x.ClearingHouse.Code == IncomingClearingHouseCode)
            .ToListAsync(ct);

        var candidates = cycles
            .Where(x => ExtractCycleNumber(x.CycleName) == cycleNumber)
            .OrderBy(x => x.CutoffTime)
            .ToList();
        if (candidates.Count != 1)
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_CYCLE_AMBIGUOUS: se esperaba un ciclo {IncomingClearingHouseCode} {cycleNumber} para {operationalDate:yyyy-MM-dd} y se encontraron {candidates.Count}.");
        }

        return candidates[0];
    }

    private async Task<AchBatch> ResolveOrCreateSyntheticBatchAsync(
        AchCycle cycle,
        CompanyEntryDescriptionCatalog companyEntryDescription,
        string scenarioKey,
        string externalOriginRouting,
        decimal authorizedAmount,
        CancellationToken ct)
    {
        var batches = await _context.AchBatches
            .Include(x => x.Transactions)
            .Where(x => x.AchCycleId == cycle.Id && x.CompanyIdentification == scenarioKey)
            .ToListAsync(ct);

        if (batches.Count > 1)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_BATCH_AMBIGUOUS: existe más de un batch sintético para la clave del escenario.");
        }

        var batch = batches.SingleOrDefault();
        if (batch is not null)
        {
            if (batch.CompanyName != BatchCompanyName
                || batch.Transactions.Any(x => !x.TransactionExternalId.StartsWith(TransactionExternalIdPrefix, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_E2E_BATCH_NOT_SYNTHETIC: el batch encontrado contiene datos no sintéticos y no será alterado.");
            }

            return batch;
        }

        var nextBatchSequence = (await _context.AchBatches
            .Where(x => x.AchCycleId == cycle.Id)
            .MaxAsync(x => (int?)x.BatchSequenceNumber, ct) ?? 0) + 1;

        batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            ServiceClassCode = IncomingServiceClassCode,
            CompanyName = BatchCompanyName,
            CompanyIdentification = scenarioKey,
            CompanyEntryDescription = companyEntryDescription.Term,
            CompanyEntryDescriptionId = companyEntryDescription.Id,
            OriginOrOdfi = externalOriginRouting,
            EffectiveEntryDate = cycle.ProcessingDate.Date,
            BatchSequenceNumber = nextBatchSequence,
            TotalDebitAmount = 0m,
            TotalCreditAmount = authorizedAmount
        };
        _context.AchBatches.Add(batch);
        return batch;
    }

    private static void ValidateExistingTransaction(
        AchTransaction transaction,
        FinancialInstitution cfa,
        FinancialInstitution external,
        AchCycle cycle,
        string receiverAccount,
        decimal amount,
        string receivingDfi,
        string externalDfi,
        string scenarioKey)
    {
        var valid = transaction.TransactionExternalId == scenarioKey
                    && transaction.Reference == scenarioKey
                    && transaction.Type == TransactionTypeEnum.Credit
                    && transaction.TransactionCode == IncomingTransactionCode
                    && transaction.Amount == amount
                    && transaction.SourceInstitutionId == external.Id
                    && transaction.DestinationInstitutionId == cfa.Id
                    && transaction.DestinationAccountNumber == receiverAccount
                    && transaction.RecipientIdNumber == SyntheticRecipientId
                    && transaction.OriginatingDFI == externalDfi
                    && transaction.ReceivingDFI == receivingDfi
                    && transaction.AchCycleId == cycle.Id
                    && transaction.EffectiveEntryDate.Date == cycle.ProcessingDate.Date
                    && transaction.State == AchTransferStateEnum.Pending
                    && transaction.AchBatch is not null
                    && transaction.AchBatch.CompanyName == BatchCompanyName;

        if (!valid)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_E2E_TRANSACTION_MISMATCH: la transacción con marca sintética no coincide con CFA, origen, cuenta, monto, ciclo o estado autorizados; no será modificada.");
        }
    }

    private static string ResolveDfi(FinancialInstitution institution, string label)
    {
        var routing = (institution.RoutingNumber ?? string.Empty).Trim();
        var transit = (institution.TransitCode ?? string.Empty).Trim();
        var baseRouting = $"{routing}{transit}";
        if (baseRouting.Length != 8 || baseRouting.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_DFI_INVALID: el routing de {label} debe tener exactamente 8 dígitos.");
        }

        var calculatedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(baseRouting);
        var storedCheckDigit = (institution.CheckDigit ?? string.Empty).Trim();
        if (storedCheckDigit != calculatedCheckDigit)
        {
            throw new InvalidOperationException(
                $"PROC_TRANSACCIONES_E2E_CHECK_DIGIT_MISMATCH: el dígito persistido de {label} no coincide con la regla canónica.");
        }

        return $"{baseRouting}{storedCheckDigit}";
    }

    private static string BuildScenarioKey(string account, long amountInCents, int cfaId, int externalId, string cycleId)
    {
        var raw = $"{account}|{amountInCents}|{cfaId}|{externalId}|{cycleId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return $"{TransactionExternalIdPrefix}{hash[..24]}";
    }

    private static string BuildSyntheticTrace(string externalOriginRouting, string scenarioKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(scenarioKey));
        var value = BitConverter.ToUInt32(hash, 0) % 6_999_999 + 1;
        return $"{externalOriginRouting}{value:0000000}";
    }

    private static int ParseTraceSequence(string traceNumber)
        => int.Parse(traceNumber[^7..], CultureInfo.InvariantCulture);

    private static int? ExtractCycleNumber(string? cycleName)
    {
        var digits = new string((cycleName ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : null;
    }

    private static string Mask(string value)
        => value.Length <= 4
            ? "****"
            : $"{new string('*', Math.Max(4, value.Length - 4))}{value[^4..]}";
}
