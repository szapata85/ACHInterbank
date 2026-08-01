using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class IncomingNachaLocalLivePreparationService : IIncomingNachaLocalLivePreparationService
{
    private const string CreatedBy = "local-live-proc-transacciones";
    private readonly AchDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public IncomingNachaLocalLivePreparationService(
        AchDbContext context,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task EnsureAsync(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        IncomingNachaFunctionalClass functionalClass,
        CancellationToken ct = default)
    {
        if (!IsEnabled()
            || functionalClass != IncomingNachaFunctionalClass.CreditoEntrante
            || !string.Equals(entry.TransactionCode, "32", StringComparison.Ordinal))
        {
            return;
        }

        var resolvedClearingHouseCode = ingestion.ResolvedClearingHouseId.HasValue
            ? await _context.ClearingHouses
                .AsNoTracking()
                .Where(x => x.Id == ingestion.ResolvedClearingHouseId.Value)
                .Select(x => x.Code)
                .SingleOrDefaultAsync(ct)
            : null;
        var clearingHouseCode = resolvedClearingHouseCode?.Trim().ToUpperInvariant();
        if (clearingHouseCode is not ("CENIT" or "ACHCOL"))
        {
            return;
        }

        var trace = RequireDigits(entry.SequenceNumber, 15, "TRACE");
        var marker = "local-live-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace))).ToLowerInvariant()[..20];
        var localCompanyIdentification = BuildLocalCompanyIdentification(trace);
        var localCompanyName = "LOCAL LIVE " + clearingHouseCode;
        var existing = await _context.AchTransactions
            .Include(x => x.AchBatch)
            .SingleOrDefaultAsync(x => x.TraceNumber == trace, ct);
        if (existing is not null)
        {
            if (existing.State is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified)
            {
                return;
            }

            // A prior local preparation used the technical marker here.  It is
            // intentionally longer than the WCF IDORIG segment, so normalize
            // only records created by this local flow before resuming dispatch.
            if (string.Equals(existing.TransactionExternalId, marker, StringComparison.Ordinal)
                && !string.Equals(existing.CompanyIdentification, localCompanyIdentification, StringComparison.Ordinal))
            {
                existing.CompanyIdentification = localCompanyIdentification;
                if (existing.AchBatch is not null
                    && string.Equals(existing.AchBatch.CompanyIdentification, marker, StringComparison.Ordinal))
                {
                    existing.AchBatch.CompanyIdentification = localCompanyIdentification;
                }

                await _context.SaveChangesAsync(ct);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(ingestion.ResolvedAchCycleId))
        {
            throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_CYCLE_MISSING: la ingesta no resolvió un ciclo.");
        }

        var cycle = await _context.AchCycles
            .Include(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == ingestion.ResolvedAchCycleId, ct)
            ?? throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_CYCLE_MISSING: no existe el ciclo resuelto.");
        if (!string.Equals(cycle.ClearingHouse?.Code, clearingHouseCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_CLEARING_HOUSE_INVALID: el ciclo debe pertenecer a la cámara resuelta.");
        }

        var batch = await _context.BatchHeaders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.NachaID == entry.NachaID && x.BatchNumber == entry.BatchNumber, ct)
            ?? throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_BATCH_MISSING: no existe BatchHeader para la entrada.");
        var originatorCode = RequireDigits(batch.OriginParticipantEntityCode, 8, "BCOORIG");
        var receiverCode = RequireDigits(entry.ReceivingParticipantEntityCode, 8, "BCORECEP");
        var destination = await ResolveInstitutionAsync(receiverCode, requireDefaultSource: true, clearingHouseCode, ct);
        var source = await ResolveInstitutionAsync(originatorCode, requireDefaultSource: false, clearingHouseCode, ct);
        var description = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_DESCRIPTION_MISSING: no existe CompanyEntryDescription activo.");

        var localBatch = await _context.AchBatches
            .Include(x => x.Transactions)
            .SingleOrDefaultAsync(x => x.AchCycleId == cycle.Id && x.CompanyIdentification == marker, ct);
        if (localBatch is null)
        {
            var nextSequence = (await _context.AchBatches
                .Where(x => x.AchCycleId == cycle.Id)
                .MaxAsync(x => (int?)x.BatchSequenceNumber, ct) ?? 0) + 1;
            localBatch = new AchBatch
            {
                AchCycleId = cycle.Id,
                ServiceClassCode = batch.ServiceClassCode ?? "220",
                CompanyName = localCompanyName,
                CompanyIdentification = localCompanyIdentification,
                CompanyEntryDescription = description.Term,
                CompanyEntryDescriptionId = description.Id,
                OriginOrOdfi = originatorCode,
                EffectiveEntryDate = cycle.ProcessingDate.Date,
                BatchSequenceNumber = nextSequence,
                TotalCreditAmount = entry.Amount ?? 0m
            };
            _context.AchBatches.Add(localBatch);
        }

        var transaction = new AchTransaction
        {
            Amount = entry.Amount ?? 0m,
            TransactionExternalId = marker,
            Reference = marker,
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "32",
            ServiceClassCode = batch.ServiceClassCode ?? "220",
            CompanyEntryDescriptionId = description.Id,
            CompanyName = localCompanyName,
            CompanyIdentification = localCompanyIdentification,
            OriginatingDFI = originatorCode + source.CheckDigit,
            ReceivingDFI = receiverCode + RequireDigits(entry.CheckDigit, 1, "RECEIVING_CHECK_DIGIT"),
            TraceNumber = trace,
            TraceSequenceNumber = int.Parse(trace[8..]),
            EffectiveEntryDate = cycle.ProcessingDate.Date,
            AddendaRecordIndicator = string.Equals(entry.AddendumIndicator, "1", StringComparison.Ordinal),
            State = AchTransferStateEnum.Pending,
            StateChangedAtUtc = DateTime.UtcNow,
            RecipientIdNumber = entry.RecipIdNumber ?? string.Empty,
            DiscretionaryData = entry.DiscreData ?? string.Empty,
            SourceAccountNumber = entry.AccountNumber?.Trim() ?? string.Empty,
            DestinationAccountNumber = entry.AccountNumber?.Trim() ?? string.Empty,
            SourceInstitutionId = source.Id,
            DestinationInstitutionId = destination.Id,
            AchCycleId = cycle.Id,
            AchBatch = localBatch
        };
        _context.AchTransactions.Add(transaction);
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = entry.EntryDetailID,
            EventType = "LocalLiveTransactionPrepared",
            EventStatus = "Created",
            Message = "Datos locales preparados para Proc_Transacciones.",
            EvidenceJson = $"{{\"createdBy\":\"{CreatedBy}\",\"clearingHouse\":\"{clearingHouseCode}\"}}",
            OccurredAtUtc = DateTime.UtcNow,
            RaisedBy = CreatedBy
        });
        await _context.SaveChangesAsync(ct);
    }

    private bool IsEnabled()
        => _environment.IsDevelopment()
           && string.Equals(_configuration["RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration["ALLOW_LOCAL_MONETARY_SOAP_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration["ProcTransacciones:Mode"], "Live", StringComparison.OrdinalIgnoreCase);

    private async Task<FinancialInstitution> ResolveInstitutionAsync(
        string participantCode,
        bool requireDefaultSource,
        string clearingHouseCode,
        CancellationToken ct)
    {
        var institutions = await _context.FinancialInstitutions
            .Where(x => x.RoutingNumber + x.TransitCode == participantCode)
            .ToListAsync(ct);
        if (institutions.Count > 1)
        {
            throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_INSTITUTION_AMBIGUOUS: el participante NACHA no es único.");
        }

        var institution = institutions.SingleOrDefault();
        if (institution is null && requireDefaultSource)
        {
            throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_CFA_MISSING: la CFA receptora debe existir y ser canónica.");
        }
        if (institution is null)
        {
            institution = new FinancialInstitution
            {
                Name = clearingHouseCode + " LOCAL EXTERNAL " + participantCode[^3..],
                RoutingNumber = participantCode[..5],
                TransitCode = participantCode[5..],
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            institution.CalculateCheckDigit();
            _context.FinancialInstitutions.Add(institution);
            await _context.SaveChangesAsync(ct);
        }

        if (institution.Status != FinancialInstitutionStatus.Active || (requireDefaultSource && !institution.IsDefaultSource))
        {
            throw new InvalidOperationException("LOCAL_LIVE_PREPARATION_CFA_INVALID: la institución receptora no es la CFA activa requerida.");
        }
        return institution;
    }

    private static string RequireDigits(string? value, int length, string name)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length != length || normalized.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException($"LOCAL_LIVE_PREPARATION_{name}_INVALID: se esperaban {length} dígitos.");
        }
        return normalized;
    }

    private static string BuildLocalCompanyIdentification(string trace)
        => "L" + trace[^14..];
}
