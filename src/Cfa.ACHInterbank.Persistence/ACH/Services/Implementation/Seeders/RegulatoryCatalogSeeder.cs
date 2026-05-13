using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
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
        // Fase 2.3B separará códigos y políticas por cámara.
        // En esta fase solo se elimina la resolución global silenciosa.
        await UpsertReturnCodesAsync(clearingHouseIds.CenitId);
        await UpsertFileRejectionCodesAsync();
        await UpsertTransactionTypePoliciesAsync();
        await UpsertReturnPoliciesAsync(clearingHouseIds.CenitId);
        await UpsertReturnOfReturnPoliciesAsync(clearingHouseIds.CenitId);
        await UpsertPrenotificationPoliciesAsync();

        await _context.SaveChangesAsync();
    }

    private async Task UpsertReturnCodesAsync(int clearingHouseId)
    {
        var desired = BuildReturnCodes(clearingHouseId).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnCodes.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.Code, out var model))
            {
                continue;
            }

            row.Description = model.Description;
            row.AppliesToDebit = model.AppliesToDebit;
            row.AppliesToCredit = model.AppliesToCredit;
            row.AppliesToPrenotification = model.AppliesToPrenotification;
            row.AppliesToReturn = model.AppliesToReturn;
            row.RequiresAddenda = model.RequiresAddenda;
            row.MaxDaysAllowed = model.MaxDaysAllowed;
            row.RegulatorySource = model.RegulatorySource;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.Code, x.Code, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnCodes.Add(model);
        }
    }

    private async Task UpsertFileRejectionCodesAsync()
    {
        var desired = BuildFileRejectionCodes().ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchFileRejectionCodes.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.Code, out var model))
            {
                continue;
            }

            row.Description = model.Description;
            row.Severity = model.Severity;
            row.AppliesToStage = model.AppliesToStage;
            row.IsRetryable = model.IsRetryable;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.Code, x.Code, StringComparison.OrdinalIgnoreCase))))
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

    private async Task UpsertReturnPoliciesAsync(int clearingHouseId)
    {
        var desired = BuildReturnPolicies(clearingHouseId).ToDictionary(x => x.TransactionType, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnPolicies.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.TransactionType, out var model))
            {
                continue;
            }

            row.AllowedReturnCodesCsv = model.AllowedReturnCodesCsv;
            row.MaxDays = model.MaxDays;
            row.RequiredOriginalTransactionState = model.RequiredOriginalTransactionState;
            row.AllowsReturnOfReturn = model.AllowsReturnOfReturn;
            row.RequiresAddenda = model.RequiresAddenda;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.TransactionType, x.TransactionType, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnPolicies.Add(model);
        }
    }

    private async Task UpsertReturnOfReturnPoliciesAsync(int clearingHouseId)
    {
        var desired = BuildReturnOfReturnPolicies(clearingHouseId).ToDictionary(x => x.OriginalReturnCode, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AchReturnOfReturnPolicies.ToListAsync();

        foreach (var row in existing)
        {
            if (!desired.TryGetValue(row.OriginalReturnCode, out var model))
            {
                continue;
            }

            row.AllowedNewReturnCodesCsv = model.AllowedNewReturnCodesCsv;
            row.MaxDays = model.MaxDays;
            row.RequiredOriginalState = model.RequiredOriginalState;
            row.IsUniquePerTransaction = model.IsUniquePerTransaction;
            row.IsActive = model.IsActive;
        }

        foreach (var model in desired.Values.Where(x => existing.All(e => !string.Equals(e.OriginalReturnCode, x.OriginalReturnCode, StringComparison.OrdinalIgnoreCase))))
        {
            _context.AchReturnOfReturnPolicies.Add(model);
        }
    }


    private async Task<(int CenitId, int AchColombiaId)> ResolveReturnClearingHouseIdsAsync()
    {
        var clearingHouses = await _context.ClearingHouses.AsNoTracking().ToListAsync();

        var cenitId = clearingHouses
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.Name))
            .Where(x => (x.Code ?? string.Empty).Contains("CENIT", StringComparison.OrdinalIgnoreCase)
                        || (x.Name ?? string.Empty).Contains("CENIT", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .FirstOrDefault();

        if (cenitId == 0)
        {
            throw new InvalidOperationException("No existe ClearingHouse CENIT para sembrar catálogos regulatorios de devolución.");
        }

        var achColombiaId = clearingHouses
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.Name))
            .Where(x => (x.Code ?? string.Empty).Contains("ACH", StringComparison.OrdinalIgnoreCase)
                        || (x.Name ?? string.Empty).Contains("ACH", StringComparison.OrdinalIgnoreCase)
                        || (x.Name ?? string.Empty).Contains("ACH COLOMBIA", StringComparison.OrdinalIgnoreCase)
                        || (x.Name ?? string.Empty).Contains("ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .FirstOrDefault(id => id != cenitId);

        if (achColombiaId == 0)
        {
            throw new InvalidOperationException("No existe ClearingHouse ACH Colombia para sembrar catálogos regulatorios de devolución.");
        }

        return (cenitId, achColombiaId);
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

    private static IEnumerable<AchReturnCode> BuildReturnCodes(int clearingHouseId)
    {
        return new[]
        {
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R01", Description = "Fondos insuficientes", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R02", Description = "Cuenta cerrada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R03", Description = "Cuenta no localizada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R04", Description = "Número de cuenta inválido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R06", Description = "Transacción retornada por solicitud de ODFI", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R07", Description = "Autorización revocada por el cliente", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R08", Description = "Pago detenido", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R09", Description = "No cobrable", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R10", Description = "Cliente informa no autorización", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R12", Description = "Sucursal vendida", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R13", Description = "Número de ruta inválido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R14", Description = "Representante/beneficiario fallecido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R15", Description = "Beneficiario o titular fallecido", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R16", Description = "Cuenta bloqueada", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R17", Description = "Criterio de edición", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R20", Description = "Cuenta no transaccional", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R23", Description = "Entrada rechazada por receptor", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "CENIT" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R29", Description = "Asesor corporativo no autorizado", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 1, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "R31", Description = "Entrada permitida de retorno", AppliesToDebit = true, AppliesToCredit = true, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 15, IsActive = true, RegulatorySource = "ACH" },
            new AchReturnCode { ClearingHouseId = clearingHouseId, FlowType = AchReturnFlowType.Any, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Code = "DEV14", Description = "No consentimiento / retorno de débito por operador", AppliesToDebit = true, AppliesToCredit = false, AppliesToPrenotification = false, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true, RegulatorySource = "OPERADOR" }
        };
    }

    private static IEnumerable<AchFileRejectionCode> BuildFileRejectionCodes()
    {
        return new[]
        {
            new AchFileRejectionCode { Code = "D01", Description = "Archivo duplicado detectado por hash/tamaño.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "D02", Description = "Formato/estructura de archivo inválida o padding no conforme.", Severity = "Fatal", AppliesToStage = "Parser", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "D03", Description = "Operador o canal de transmisión incorrecto.", Severity = "Fatal", AppliesToStage = "Transmission", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "D04", Description = "Inconsistencia de secuencia, batch count o conteos físicos.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "D05", Description = "Control hash/cuadres NACHA inválidos.", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "D06", Description = "Campo obligatorio ausente o registro fuera de orden esperado.", Severity = "Fatal", AppliesToStage = "Parser", IsRetryable = false, IsActive = true },
            new AchFileRejectionCode { Code = "I500", Description = "Error técnico de integración externa (HTTP 500).", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true },
            new AchFileRejectionCode { Code = "I503", Description = "Servicio externo no disponible (HTTP 503).", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true },
            new AchFileRejectionCode { Code = "ITIMEOUT", Description = "Timeout técnico en integración SOAP.", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true },
            new AchFileRejectionCode { Code = "ISOAP", Description = "SOAP fault técnico recuperable.", Severity = "Error", AppliesToStage = "Integration", IsRetryable = true, IsActive = true },
            new AchFileRejectionCode { Code = "IFUNC", Description = "Rechazo funcional de integración no reintentable automáticamente.", Severity = "Warning", AppliesToStage = "Integration", IsRetryable = false, IsActive = true }
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

    private static IEnumerable<AchReturnPolicy> BuildReturnPolicies(int clearingHouseId)
    {
        return new[]
        {
            new AchReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType = "Debit", AllowedReturnCodesCsv = "R01,R02,R03,R04,R06,R07,R08,R09,R10,R12,R13,R14,R15,R16,R17,R20,R23,R29,R31,DEV14", MaxDays = 60, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true },
            new AchReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType = "Credit", AllowedReturnCodesCsv = "R03,R04,R20,R23,R31", MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true },
            new AchReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType = "Prenotification", AllowedReturnCodesCsv = "R03,R29", MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = false, RequiresAddenda = true, IsActive = true },
            new AchReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType = "Return", AllowedReturnCodesCsv = "R01,R02,R03,R09,R10", MaxDays = 15, RequiredOriginalTransactionState = "ReturnedByEpr", AllowsReturnOfReturn = true, RequiresAddenda = true, IsActive = true }
        };
    }

    private static IEnumerable<AchReturnOfReturnPolicy> BuildReturnOfReturnPolicies(int clearingHouseId)
    {
        return new[]
        {
            new AchReturnOfReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.ReturnOfReturn, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), OriginalReturnCode = "R01", AllowedNewReturnCodesCsv = "R02,R03,R09", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true },
            new AchReturnOfReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.ReturnOfReturn, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), OriginalReturnCode = "R02", AllowedNewReturnCodesCsv = "R03,R10", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true },
            new AchReturnOfReturnPolicy { ClearingHouseId = clearingHouseId, Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.ReturnOfReturn, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), OriginalReturnCode = "R03", AllowedNewReturnCodesCsv = "R03,R31", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true, IsActive = true }
        };
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
}
