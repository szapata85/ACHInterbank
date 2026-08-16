using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class RegulatoryCatalogSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public RegulatoryCatalogSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 3;

    public async Task SeedAsync()
    {
        var clearingHouseIds = await ResolveReturnClearingHouseIdsAsync();
        await UpsertReturnCodesAsync(clearingHouseIds);
        await UpsertFileRejectionCodesAsync(clearingHouseIds);
        await UpsertTransactionTypePoliciesAsync();
        await UpsertReturnPoliciesAsync(clearingHouseIds);
        await UpsertReturnOfReturnPoliciesAsync(clearingHouseIds);
        await UpsertPrenotificationPoliciesAsync();
        await UpsertClearingHouseTransactionRulesAsync(clearingHouseIds);

        await _context.SaveChangesAsync();
    }

    private async Task UpsertReturnCodesAsync((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        // Fase 2.3B-2 separará políticas por cámara.
        // En esta fase solo se separan códigos de devolución.
        var desired = BuildReturnCodes(clearingHouseIds)
            .ToDictionary(x => $"{x.ClearingHouseId}|{x.Code}|{x.FlowType}", StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnCodes.ToListAsync();

        foreach (var legacyR96 in existing.Where(x =>
                     string.Equals(x.Code, "R96", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(x.FlowType, AchReturnFlowType.Any, StringComparison.OrdinalIgnoreCase)
                     && !x.AppliesToReturn
                     && x.EffectiveFrom.Year <= 2000))
        {
            legacyR96.IsActive = false;
            legacyR96.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
            legacyR96.RegulatorySource = "R96_INTEGRATION_ONLY";
        }

        foreach (var row in existing)
        {
            var key = $"{row.ClearingHouseId}|{row.Code}|{row.FlowType}";
            if (!desired.TryGetValue(key, out var model))
            {
                continue;
            }

            row.Description = model.Description;
            row.BusinessOutcome = model.BusinessOutcome;
            row.AppliesToDebit = model.AppliesToDebit;
            row.AppliesToCredit = model.AppliesToCredit;
            row.AppliesToPrenotification = model.AppliesToPrenotification;
            row.AppliesToReturn = model.AppliesToReturn;
            row.RequiresAddenda = model.RequiresAddenda;
            row.MaxDaysAllowed = model.MaxDaysAllowed;
            row.RegulatorySource = model.RegulatorySource;
            row.IsActive = model.IsActive;
            row.EffectiveFrom = model.EffectiveFrom;
            row.EffectiveTo = model.EffectiveTo;
            row.FlowType = model.FlowType;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e =>
                     e.ClearingHouseId != x.ClearingHouseId
                     || !string.Equals(e.Code, x.Code, StringComparison.OrdinalIgnoreCase)
                     || !string.Equals(e.FlowType, x.FlowType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnCodes.Add(model);
        }
    }

    private async Task UpsertFileRejectionCodesAsync((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        var desired = BuildFileRejectionCodes(clearingHouseIds)
            .ToDictionary(x => $"{x.ClearingHouseId}|{x.Code}|{x.AppliesToStage}", StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchFileRejectionCodes.ToListAsync();

        foreach (var row in existing)
        {
            if (!row.ClearingHouseId.HasValue && row.Code.StartsWith('D'))
            {
                row.ClearingHouseId = clearingHouseIds.CenitId;
            }

            var key = $"{row.ClearingHouseId}|{row.Code}|{row.AppliesToStage}";
            if (!desired.TryGetValue(key, out var model))
            {
                model = desired.Values.SingleOrDefault(x =>
                    x.ClearingHouseId == row.ClearingHouseId
                    && string.Equals(x.Code, row.Code, StringComparison.OrdinalIgnoreCase));
                if (model is null)
                {
                    continue;
                }
            }

            row.Description = model.Description;
            row.Severity = model.Severity;
            row.AppliesToStage = model.AppliesToStage;
            row.IsRetryable = model.IsRetryable;
            row.IsActive = model.IsActive;
            row.EffectiveFrom = model.EffectiveFrom;
            row.EffectiveTo = model.EffectiveTo;
            row.RegulatorySource = model.RegulatorySource;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e =>
                     e.ClearingHouseId != x.ClearingHouseId
                     || !string.Equals(e.Code, x.Code, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchFileRejectionCodes.Add(model);
        }
    }

    private async Task UpsertTransactionTypePoliciesAsync()
    {
        var desired = BuildTransactionTypePolicies().ToDictionary(x => x.TransactionType, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchTransactionTypePolicies.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.TransactionType, out var model))
            {
                continue;
            }

            row.PriorityOrder = model.PriorityOrder;
            row.IsMonetary = model.IsMonetary;
            row.RequiresPrenotification = model.RequiresPrenotification;
            row.CanBeReturned = model.CanBeReturned;
            row.CanBeReturnedAgain = model.CanBeReturnedAgain;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.TransactionType, x.TransactionType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchTransactionTypePolicies.Add(model);
        }
    }

    private async Task UpsertReturnPoliciesAsync((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        // Fase 2.3B-3 separará políticas de devolución de devolución por cámara.
        // En esta fase solo se separan políticas de devolución.
        var desiredCodes = BuildReturnCodes(clearingHouseIds).ToList();
        var desired = BuildReturnPolicies(clearingHouseIds, desiredCodes)
            .ToDictionary(x => $"{x.ClearingHouseId}|{x.TransactionType}|{x.Direction}|{x.FlowType}", StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnPolicies.ToListAsync();

        foreach (var row in existing)
        {
            var key = $"{row.ClearingHouseId}|{row.TransactionType}|{row.Direction}|{row.FlowType}";
            if (!desired.TryGetValue(key, out var model))
            {
                continue;
            }

            row.AllowedReturnCodesCsv = model.AllowedReturnCodesCsv;
            row.MaxDays = model.MaxDays;
            row.RequiredOriginalTransactionState = model.RequiredOriginalTransactionState;
            row.AllowsReturnOfReturn = model.AllowsReturnOfReturn;
            row.RequiresAddenda = model.RequiresAddenda;
            row.IsActive = model.IsActive;
            row.EffectiveFrom = model.EffectiveFrom;
            row.EffectiveTo = model.EffectiveTo;
            row.Direction = model.Direction;
            row.FlowType = model.FlowType;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e =>
                     e.ClearingHouseId != x.ClearingHouseId
                     || !string.Equals(e.TransactionType, x.TransactionType, StringComparison.OrdinalIgnoreCase)
                     || !string.Equals(e.Direction, x.Direction, StringComparison.OrdinalIgnoreCase)
                     || !string.Equals(e.FlowType, x.FlowType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnPolicies.Add(model);
        }
    }

    private async Task UpsertReturnOfReturnPoliciesAsync((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        // Fase 2.3B completa: códigos, políticas y devolución de devolución quedan separados por cámara.
        var desiredCodes = BuildReturnCodes(clearingHouseIds).ToList();
        var desired = BuildReturnOfReturnPolicies(clearingHouseIds, desiredCodes)
            .ToDictionary(x => $"{x.ClearingHouseId}|{x.OriginalReturnCode}|{x.Direction}|{x.FlowType}", StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnOfReturnPolicies.ToListAsync();

        foreach (var row in existing)
        {
            var key = $"{row.ClearingHouseId}|{row.OriginalReturnCode}|{row.Direction}|{row.FlowType}";
            if (!desired.TryGetValue(key, out var model))
            {
                continue;
            }

            row.AllowedNewReturnCodesCsv = model.AllowedNewReturnCodesCsv;
            row.MaxDays = model.MaxDays;
            row.RequiredOriginalState = model.RequiredOriginalState;
            row.IsUniquePerTransaction = model.IsUniquePerTransaction;
            row.IsActive = model.IsActive;
            row.EffectiveFrom = model.EffectiveFrom;
            row.EffectiveTo = model.EffectiveTo;
            row.Direction = model.Direction;
            row.FlowType = model.FlowType;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e =>
                     e.ClearingHouseId != x.ClearingHouseId
                     || !string.Equals(e.OriginalReturnCode, x.OriginalReturnCode, StringComparison.OrdinalIgnoreCase)
                     || !string.Equals(e.Direction, x.Direction, StringComparison.OrdinalIgnoreCase)
                     || !string.Equals(e.FlowType, x.FlowType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnOfReturnPolicies.Add(model);
        }
    }

    private async Task<(int CenitId, int AchColombiaId)> ResolveReturnClearingHouseIdsAsync()
    {
        var clearingHouses = await _context.ClearingHouses
            .AsNoTracking()
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var cenit = clearingHouses.FirstOrDefault(x =>
            string.Equals(x.Code, "CENIT", StringComparison.OrdinalIgnoreCase));
        if (cenit is null)
        {
            throw new InvalidOperationException("No existe ClearingHouse CENIT para sembrar catálogos regulatorios de devolución.");
        }

        var achColombia = clearingHouses.FirstOrDefault(x =>
            string.Equals(x.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
            ?? clearingHouses.FirstOrDefault(x =>
                string.Equals(x.Code, "ACH", StringComparison.OrdinalIgnoreCase));
        if (achColombia is null)
        {
            throw new InvalidOperationException("No existe ClearingHouse ACH Colombia para sembrar catálogos regulatorios de devolución.");
        }

        return (cenit.Id, achColombia.Id);
    }

    private async Task UpsertPrenotificationPoliciesAsync()
    {
        var desired = BuildPrenotificationPolicies().ToDictionary(x => x.TransactionType, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchPrenotificationPolicies.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.TransactionType, out var model))
            {
                continue;
            }

            row.IsRequired = model.IsRequired;
            row.RequiresAddenda = model.RequiresAddenda;
            row.BlocksMonetaryTransactionIfMissing = model.BlocksMonetaryTransactionIfMissing;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.TransactionType, x.TransactionType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchPrenotificationPolicies.Add(model);
        }
    }

    private async Task UpsertClearingHouseTransactionRulesAsync((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        var desired = BuildClearingHouseTransactionRules(clearingHouseIds)
            .ToDictionary(x => $"{x.ClearingHouseId}|{x.TransactionNature}|{x.TransactionType}|{x.EffectiveFrom:yyyyMMdd}", StringComparer.OrdinalIgnoreCase);
        var existing = await _context.ClearingHouseTransactionRules.ToListAsync();

        foreach (var model in desired.Values.Where(x => existing.All(e =>
                     e.ClearingHouseId != x.ClearingHouseId
                     || e.TransactionNature != x.TransactionNature
                     || e.TransactionType != x.TransactionType
                     || e.EffectiveFrom.Date != x.EffectiveFrom.Date)))
        {
            _context.ClearingHouseTransactionRules.Add(model);
        }
    }

    private static IEnumerable<AchReturnCode> BuildReturnCodes((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        var rows = new[]
        {
            new AchReturnCode { Code = "R01", Description = "Fondos insuficientes", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R02", Description = "Cuenta cerrada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R03", Description = "Cuenta no localizada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R04", Description = "Número de cuenta inválido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R06", Description = "Transacción retornada por solicitud de ODFI", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R07", Description = "Autorización revocada por el cliente", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { Code = "R08", Description = "Pago detenido", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R09", Description = "No cobrable", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R10", Description = "Cliente informa no autorización", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { Code = "R12", Description = "Sucursal vendida", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R13", Description = "Número de ruta inválido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R14", Description = "Representante/beneficiario fallecido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R15", Description = "Beneficiario o titular fallecido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R16", Description = "Cuenta bloqueada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R17", Description = "Criterio de edición", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R20", Description = "Cuenta no transaccional", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R23", Description = "Entrada rechazada por receptor", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { Code = "R29", Description = "Asesor corporativo no autorizado", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { Code = "R31", Description = "Entrada permitida de retorno", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 15, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { Code = "DEV14", Description = "No consentimiento / retorno de débito por operador", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "OPERADOR" }
        };

        var cenitRows = CenitIncomingReturnPolicy.CauseDefinitions.Select(cause => new AchReturnCode
        {
            ClearingHouseId = clearingHouseIds.CenitId,
            Code = cause.Code,
            Description = cause.Description,
            AppliesToDebit = cause.AppliesToDebitMonetary,
            AppliesToCredit = cause.AppliesToCreditMonetary,
            AppliesToPrenotification = cause.AppliesToDebitPrenotification || cause.AppliesToCreditPrenotification,
            AppliesToReturn = true,
            RequiresAddenda = true,
            MaxDaysAllowed = cause.MaxCalendarDays,
            IsActive = true,
            EffectiveFrom = new DateTime(2023, 11, 28),
            RegulatorySource = "CENIT"
        }).ToList();
        var cenitRorRows = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["R60"] = "Devolución de una Devolución solicitada por el Participante Receptor",
            ["R61"] = "Devolución enviada al Participante incorrecto",
            ["R62"] = "Número de secuencia incorrecto",
            ["R63"] = "Valor incorrecto",
            ["R64"] = "Número de Identificación incorrecto",
            ["R65"] = "Código de Transacción incorrecto",
            ["R66"] = "Identificación del Originador incorrecta",
            ["R67"] = "Devolución duplicada",
            ["R68"] = "Devolución extemporánea",
            ["R69"] = "Múltiples errores",
            ["R70"] = "Número de cuenta incorrecto",
            ["R71"] = "Datos Discrecionales del Originador incorrectos",
            ["R72"] = "Tipo de Servicio incorrecto",
            ["R73"] = "Descripción de Lote incorrecta",
            ["R74"] = "Devolución errada de una transacción crédito monetaria por la causal R32"
        }.Select(item => new AchReturnCode
        {
            ClearingHouseId = clearingHouseIds.CenitId,
            Code = item.Key,
            FlowType = AchReturnFlowType.ReturnOfReturn,
            Description = item.Value,
            AppliesToReturn = true,
            RequiresAddenda = true,
            MaxDaysAllowed = 1,
            IsActive = true,
            EffectiveFrom = new DateTime(2026, 5, 7),
            RegulatorySource = "CENIT Anexo A T2"
        });

        var achRows = rows.Where(row => !string.Equals(row.RegulatorySource, "CENIT", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var row in achRows)
        {
            row.ClearingHouseId = clearingHouseIds.AchColombiaId;
        }

        return cenitRows.Concat(cenitRorRows).Concat(achRows);
    }

    private static IEnumerable<AchFileRejectionCode> BuildFileRejectionCodes((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        var effectiveFrom = new DateTime(2024, 1, 1);
        const string cenitSource = "CENIT DSP-152 Anexo B; MATRIZ_REGLAS_CENIT";
        return new[]
        {
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D01", Description = "El archivo está dirigido a una entidad receptora diferente de la esperada.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D02", Description = "El archivo fue firmado o cifrado para un operador receptor o usuarios no válidos.", Severity = "Fatal", AppliesToStage = "Protection", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D03", Description = "El archivo tiene formato incorrecto y no fue posible procesarlo.", Severity = "Fatal", AppliesToStage = "Parser", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D04", Description = "El archivo ya fue recibido y corresponde a un duplicado.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D05", Description = "El número de registros del nombre externo no coincide con el contenido del archivo.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { ClearingHouseId = clearingHouseIds.CenitId, Code = "D06", Description = "La distribución del archivo no corresponde al operador receptor según las reglas vigentes.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = cenitSource },
            new AchFileRejectionCode { Code = "I500", Description = "Error técnico de integración externa (HTTP 500).", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = "Catálogo técnico interno" },
            new AchFileRejectionCode { Code = "I503", Description = "Servicio externo no disponible (HTTP 503).", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = "Catálogo técnico interno" },
            new AchFileRejectionCode { Code = "ITIMEOUT", Description = "Tiempo de espera agotado en la integración SOAP.", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = "Catálogo técnico interno" },
            new AchFileRejectionCode { Code = "ISOAP", Description = "Falla técnica SOAP recuperable.", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = "Catálogo técnico interno" },
            new AchFileRejectionCode { Code = "IFUNC", Description = "Rechazo funcional de integración no reintentable automáticamente.", Severity = "Warning", AppliesToStage = "Integration", IsRetryable = false, IsActive = true, EffectiveFrom = effectiveFrom, RegulatorySource = "Catálogo técnico interno" }
        };
    }

    private static IEnumerable<AchTransactionTypePolicy> BuildTransactionTypePolicies()
    {
        return new[]
        {
            new AchTransactionTypePolicy { TransactionType = "ReturnDebit", PriorityOrder = 100, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "ReturnCredit", PriorityOrder = 95, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Debit", PriorityOrder = 90, IsMonetary = true, RequiresPrenotification = true, CanBeReturned = true, CanBeReturnedAgain = false, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Credit", PriorityOrder = 80, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = true, CanBeReturnedAgain = false, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Prenotification", PriorityOrder = 70, IsMonetary = false, RequiresPrenotification = false, CanBeReturned = true, CanBeReturnedAgain = false, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Return", PriorityOrder = 100, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "ReturnOfReturn", PriorityOrder = 60, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = false, IsActive = true }
        };
    }

    private static IEnumerable<AchReturnPolicy> BuildReturnPolicies((int CenitId, int AchColombiaId) clearingHouseIds, IReadOnlyCollection<AchReturnCode> returnCodes)
    {
        var cenitCodes = returnCodes.Where(x => x.ClearingHouseId == clearingHouseIds.CenitId).Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var achCodes = returnCodes.Where(x => x.ClearingHouseId == clearingHouseIds.AchColombiaId).Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        static string FilterCodes(string csv, HashSet<string> allowed) => string.Join(',', csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(allowed.Contains));

        return new[]
        {
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.CenitId, TransactionType = "Debit", AllowedReturnCodesCsv = FilterCodes("R01,R02,R03,R04,R06,R07,R08,R09,R10,R12,R13,R14,R15,R16,R17,R20,R23,R29,R31,DEV14", cenitCodes), MaxDays = 60, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.AchColombiaId, TransactionType = "Debit", AllowedReturnCodesCsv = FilterCodes("R01,R02,R03,R04,R06,R07,R08,R09,R10,R12,R13,R14,R15,R16,R17,R20,R23,R29,R31,DEV14", achCodes), MaxDays = 60, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.CenitId, TransactionType = "Credit", AllowedReturnCodesCsv = FilterCodes("R03,R04,R20,R23,R31", cenitCodes), MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.AchColombiaId, TransactionType = "Credit", AllowedReturnCodesCsv = FilterCodes("R03,R04,R20,R23,R31", achCodes), MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.CenitId, TransactionType = "Prenotification", AllowedReturnCodesCsv = FilterCodes("R03,R29", cenitCodes), MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = false, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.AchColombiaId, TransactionType = "Prenotification", AllowedReturnCodesCsv = FilterCodes("R03,R29", achCodes), MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = false, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.CenitId, TransactionType = "Return", AllowedReturnCodesCsv = FilterCodes("R01,R02,R03,R09,R10", cenitCodes), MaxDays = 15, RequiredOriginalTransactionState = "ReturnedByEpr", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null },
            new AchReturnPolicy { ClearingHouseId = clearingHouseIds.AchColombiaId, TransactionType = "Return", AllowedReturnCodesCsv = FilterCodes("R01,R02,R03,R09,R10", achCodes), MaxDays = 15, RequiredOriginalTransactionState = "ReturnedByEpr", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date, EffectiveTo = null }
        };
    }

    private static IEnumerable<AchReturnOfReturnPolicy> BuildReturnOfReturnPolicies((int CenitId, int AchColombiaId) clearingHouseIds, IReadOnlyCollection<AchReturnCode> returnCodes)
    {
        var cenitCodes = returnCodes.Where(x => x.ClearingHouseId == clearingHouseIds.CenitId).Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var achCodes = returnCodes.Where(x => x.ClearingHouseId == clearingHouseIds.AchColombiaId).Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        static string FilterCodes(string csv, HashSet<string> allowed) => string.Join(',', csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(allowed.Contains));

        var baseRules = new[]
        {
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R01", AllowedNewReturnCodesCsv = "R02,R03,R09", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true },
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R02", AllowedNewReturnCodesCsv = "R03,R10", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true },
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R03", AllowedNewReturnCodesCsv = "R03,R31", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true },
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R31", AllowedNewReturnCodesCsv = "R29,R10", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true }
        };

        var result = new List<AchReturnOfReturnPolicy>();

        var cenitRorCauses = FilterCodes(
            string.Join(',', Enumerable.Range(60, 15).Select(value => $"R{value}")),
            cenitCodes);
        var cenitOrdinaryCauses = new[]
        {
            "R01", "R02", "R03", "R04", "R06", "R07", "R08", "R09", "R10", "R12",
            "R13", "R14", "R15", "R16", "R17", "R20", "R23", "R29", "R31", "R32", "R33", "R34", "R35"
        };
        foreach (var originalCause in cenitOrdinaryCauses.Where(cenitCodes.Contains))
        {
            result.Add(new AchReturnOfReturnPolicy
            {
                ClearingHouseId = clearingHouseIds.CenitId,
                OriginalReturnCode = originalCause,
                AllowedNewReturnCodesCsv = cenitRorCauses,
                MaxDays = 1,
                RequiredOriginalState = "ReturnedByEpr",
                IsUniquePerTransaction = true,
                IsActive = true,
                Direction = AchReturnDirection.Any,
                FlowType = AchReturnFlowType.ReturnOfReturn,
                EffectiveFrom = DateTime.UtcNow.Date,
                EffectiveTo = null
            });
        }

        foreach (var rule in baseRules)
        {
            if (achCodes.Contains(rule.OriginalReturnCode))
            {
                var allowed = FilterCodes(rule.AllowedNewReturnCodesCsv, achCodes);
                if (!string.IsNullOrWhiteSpace(allowed))
                {
                    result.Add(new AchReturnOfReturnPolicy
                    {
                        ClearingHouseId = clearingHouseIds.AchColombiaId,
                        OriginalReturnCode = rule.OriginalReturnCode,
                        AllowedNewReturnCodesCsv = allowed,
                        MaxDays = rule.MaxDays,
                        RequiredOriginalState = rule.RequiredOriginalState,
                        IsUniquePerTransaction = rule.IsUniquePerTransaction,
                        IsActive = rule.IsActive,
                        Direction = AchReturnDirection.Any,
                        FlowType = AchReturnFlowType.ReturnOfReturn,
                        EffectiveFrom = DateTime.UtcNow.Date,
                        EffectiveTo = null
                    });
                }
            }
        }

        return result;
    }

    private static IEnumerable<AchPrenotificationPolicy> BuildPrenotificationPolicies()
    {
        return new[]
        {
            new AchPrenotificationPolicy { TransactionType = "Debit", IsRequired = true, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = true, IsActive = true },
            new AchPrenotificationPolicy { TransactionType = "Credit", IsRequired = false, RequiresAddenda = false, BlocksMonetaryTransactionIfMissing = false, IsActive = true },
            new AchPrenotificationPolicy { TransactionType = "Prenotification", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false, IsActive = true },
            new AchPrenotificationPolicy { TransactionType = "Return", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false, IsActive = true },
            new AchPrenotificationPolicy { TransactionType = "ReturnOfReturn", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false, IsActive = true }
        };
    }

    private static IEnumerable<ClearingHouseTransactionRule> BuildClearingHouseTransactionRules((int CenitId, int AchColombiaId) clearingHouseIds)
    {
        var effectiveFrom = new DateTime(2025, 1, 1);

        return new[]
        {
            new ClearingHouseTransactionRule
            {
                ClearingHouseId = clearingHouseIds.AchColombiaId,
                TransactionNature = TransactionNature.Debit,
                TransactionType = TransactionTypeEnum.Debit,
                RequiresPrenotification = true,
                PrenotificationMode = PrenotificationRequirementMode.Mandatory,
                PrenotificationLeadBusinessDays = 3,
                RequiresReceiverIdentificationValidation = true,
                ReceiverIdentificationValidationMode = ValidationRequirementMode.Mandatory,
                AppliesToNachaExport = true,
                AppliesToMonetaryTransactions = true,
                EffectiveFrom = effectiveFrom,
                IsActive = true,
                NormativeSource = "MAN-004 ACH Colombia V32",
                NormativeReference = "2.11.4, 2.11.4.1, 2.11.4.2, 2.11.6",
                Notes = "Debito ACH Colombia: prenotificacion tecnica obligatoria previa al proceso de debito; receptor valida cuenta e identificacion segun norma."
            },
            new ClearingHouseTransactionRule
            {
                ClearingHouseId = clearingHouseIds.AchColombiaId,
                TransactionNature = TransactionNature.Credit,
                TransactionType = TransactionTypeEnum.Credit,
                RequiresPrenotification = false,
                PrenotificationMode = PrenotificationRequirementMode.Optional,
                PrenotificationLeadBusinessDays = null,
                RequiresReceiverIdentificationValidation = false,
                ReceiverIdentificationValidationMode = ValidationRequirementMode.Optional,
                AppliesToNachaExport = true,
                AppliesToMonetaryTransactions = true,
                EffectiveFrom = effectiveFrom,
                IsActive = true,
                NormativeSource = "MAN-004 ACH Colombia V32",
                NormativeReference = "2.10.2, 2.10.3, 2.10.3.1, 2.10.3.2",
                Notes = "Credito ACH Colombia: prenotificacion discrecional/opcional; no bloquea exportacion monetaria si no fue enviada."
            },
            new ClearingHouseTransactionRule
            {
                ClearingHouseId = clearingHouseIds.CenitId,
                TransactionNature = TransactionNature.Debit,
                TransactionType = TransactionTypeEnum.Debit,
                RequiresPrenotification = true,
                PrenotificationMode = PrenotificationRequirementMode.Mandatory,
                PrenotificationLeadBusinessDays = null,
                RequiresReceiverIdentificationValidation = true,
                ReceiverIdentificationValidationMode = ValidationRequirementMode.Mandatory,
                AppliesToNachaExport = true,
                AppliesToMonetaryTransactions = true,
                EffectiveFrom = effectiveFrom,
                IsActive = true,
                NormativeSource = "CENIT DSP-152 Anexo 2",
                NormativeReference = "4.7 Prenotificaciones",
                Notes = "Debito CENIT: antes de una entrada debito el originador debe enviar notificacion previa/prenotificacion con addenda."
            },
            new ClearingHouseTransactionRule
            {
                ClearingHouseId = clearingHouseIds.CenitId,
                TransactionNature = TransactionNature.Credit,
                TransactionType = TransactionTypeEnum.Credit,
                RequiresPrenotification = false,
                PrenotificationMode = PrenotificationRequirementMode.Optional,
                PrenotificationLeadBusinessDays = null,
                RequiresReceiverIdentificationValidation = false,
                ReceiverIdentificationValidationMode = ValidationRequirementMode.Optional,
                AppliesToNachaExport = true,
                AppliesToMonetaryTransactions = true,
                EffectiveFrom = effectiveFrom,
                IsActive = true,
                NormativeSource = "CENIT DSP-152 Anexo 2",
                NormativeReference = "4.7 Prenotificaciones",
                Notes = "Credito CENIT: la prenotificacion credito no es obligatoria segun documento fuente."
            }
        };
    }
}
