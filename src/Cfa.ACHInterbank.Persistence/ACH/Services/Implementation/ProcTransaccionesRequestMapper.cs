using System.Globalization;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ProcTransaccionesRequestMapper : IProcTransaccionesRequestMapper
{
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly AchDbContext _context;

    public ProcTransaccionesRequestMapper(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ProcTransaccionesRequestResolution> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Transacciones" && x.IsActive, ct)
            ?? throw new InvalidOperationException("No existe IntegrationMethod activo para WSCFAACH.Proc_Transacciones.");

        var mappingSet = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No existe IntegrationMappingSet publicado para Proc_Transacciones.");

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync(ct);

        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSet.Id && x.Enabled)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            throw new InvalidOperationException($"El mapping set {mappingSet.Id} publicado no tiene reglas habilitadas.");
        }

        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            var rule = rules.FirstOrDefault(x => x.ParameterId == parameter.Id);
            if (rule is null)
            {
                continue;
            }

            var value = ResolveValue(rule, queueItem, ingestion, classification, transaction, cycle, executionDateTime);
            if (string.IsNullOrWhiteSpace(value) && parameter.Required)
            {
                throw new InvalidOperationException($"El parámetro requerido {parameter.ParameterPath} no pudo resolverse.");
            }

            resolved[parameter.ParameterPath] = value ?? string.Empty;
        }

        var requiredKeys = new[] { "TRNIDTX", "TRNVALOR", "TRNCOD" };
        foreach (var key in requiredKeys)
        {
            if (!resolved.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"El mapping publicado no resolvió {key} para Proc_Transacciones.");
            }
        }

        var snapshotHash = await _context.IntegrationMappingSetHistory
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSet.Id)
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => x.SnapshotHash)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return new ProcTransaccionesRequestResolution(
            Contract: new ProcTransaccionesRequestContract(resolved),
            MappingSetId: mappingSet.Id,
            MappingVersion: mappingSet.Version,
            MappingSnapshotHash: snapshotHash);
    }

    public string BuildSoapBody(ProcTransaccionesRequestContract request)
    {
        var operation = new XElement(ActionNamespace + "Proc_Transacciones",
            request.Parameters.Select(x => new XElement(ActionNamespace + x.Key, x.Value)));
        var envelope = new XDocument(
            new XElement(XName.Get("Envelope", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XAttribute(XNamespace.Xmlns + "soapenv", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XAttribute(XNamespace.Xmlns + "tem", ActionNamespace.NamespaceName),
                new XElement(XName.Get("Body", "http://schemas.xmlsoap.org/soap/envelope/"), operation)));
        return envelope.ToString(SaveOptions.DisableFormatting);
    }

    private static string? ResolveValue(
        IntegrationMappingRule rule,
        IncomingNachaDispatchQueue queue,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return rule.FixedValue.Trim();
        }

        var source = (rule.SourceFieldPath ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(source))
        {
            return rule.DefaultValue;
        }

        return source switch
        {
            "transaction.id" => transaction.Id.ToString(CultureInfo.InvariantCulture),
            "transaction.amount" => transaction.Amount.ToString(CultureInfo.InvariantCulture),
            "transaction.transactioncode" => transaction.TransactionCode,
            "transaction.trace" => transaction.TraceNumber,
            "transaction.externalid" => transaction.TransactionExternalId,
            "cycle.id" => cycle.Id,
            "cycle.clearinghouseid" => cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture),
            "ingestion.id" => ingestion.Id.ToString("N"),
            "classification.class" => ((int)classification.FunctionalClass).ToString(CultureInfo.InvariantCulture),
            "queue.idempotencykey" => queue.IdempotencyDispatchKey,
            "execution.datetimeutc" => executionDateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            _ => rule.DefaultValue
        };
    }
}
