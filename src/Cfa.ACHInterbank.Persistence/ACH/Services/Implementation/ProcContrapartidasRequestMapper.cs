using System.Globalization;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ProcContrapartidasRequestMapper : IProcContrapartidasRequestMapper
{
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly IProcContrapartidasFunctionalMappingResolver _functionalResolver;

    public ProcContrapartidasRequestMapper(IProcContrapartidasFunctionalMappingResolver functionalResolver)
    {
        _functionalResolver = functionalResolver;
    }

    public async Task<ProcContrapartidasRequestResolution> ResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
        {
            throw new InvalidOperationException("Proc_Contrapartidas requiere al menos una transacción.");
        }

        // Transición controlada: solo cae a fallback si no existe mapping publicado.
        var configured = await _functionalResolver.TryResolveAsync(cycle, transactions, executionDateTime, ct);

        if (configured is not null)
        {
            return configured;
        }

        return new ProcContrapartidasRequestResolution
        {
            Contract = BuildTransitionalFallback(cycle, transactions.OrderBy(t => t.Id).First(), executionDateTime),
            MappingSetId = null,
            MappingVersion = null,
            MappingSnapshotHash = string.Empty,
            UsedFallback = true
        };
    }

    public string BuildSoapBody(ProcContrapartidasRequestContract request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestContract(request);

        var body = new XElement(ActionNamespace + "Proc_Contrapartidas",
            new XElement(ActionNamespace + "OFNIT", request.OFNIT),
            new XElement(ActionNamespace + "OFEMP", request.OFEMP),
            new XElement(ActionNamespace + "OFCTA", request.OFCTA),
            new XElement(ActionNamespace + "OFDD", request.OFDD),
            new XElement(ActionNamespace + "OFFECHEFEC", request.OFFECHEFEC),
            new XElement(ActionNamespace + "OFMONDEB", request.OFMONDEB.ToString(CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "OFMONCRE", request.OFMONCRE.ToString(CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "OFIDARCH", request.OFIDARCH),
            new XElement(ActionNamespace + "OFIDLOT", request.OFIDLOT),
            new XElement(ActionNamespace + "OFST", request.OFST),
            new XElement(ActionNamespace + "OFIDTX", request.OFIDTX),
            new XElement(ActionNamespace + "OFIDREVER", request.OFIDREVER),
            new XElement(ActionNamespace + "OFIDEBAPLI", request.OFIDEBAPLI),
            new XElement(ActionNamespace + "OFIDCAMCOMPE", request.OFIDCAMCOMPE),
            new XElement(ActionNamespace + "OFDIRECCIONIP", request.OFDIRECCIONIP),
            new XElement(ActionNamespace + "OFLIBRE", request.OFLIBRE),
            new XElement(ActionNamespace + "OFLIBRE1", request.OFLIBRE1),
            new XElement(ActionNamespace + "ANSIDLOTE", request.ANSIDLOTE),
            new XElement(ActionNamespace + "ANSST", request.ANSST),
            new XElement(ActionNamespace + "ANCLC", request.ANCLC),
            new XElement(ActionNamespace + "ANSIDTX", request.ANSIDTX),
            new XElement(ActionNamespace + "ANSIDREVER", request.ANSIDREVER));

        return body.ToString(SaveOptions.DisableFormatting);
    }

    private static ProcContrapartidasRequestContract BuildTransitionalFallback(
        AchCycle cycle,
        AchTransaction tx,
        DateTime executionDateTime)
    {
        var isDebit = tx.Type.ToString().Equals("Debit", StringComparison.OrdinalIgnoreCase);

        return new ProcContrapartidasRequestContract
        {
            OFNIT = tx.CompanyIdentification ?? string.Empty,
            OFEMP = (cycle.ClearingHouse?.Code ?? "ACH").Trim(),
            OFCTA = tx.SourceAccountNumber ?? string.Empty,
            OFDD = isDebit ? "D" : "C",
            OFFECHEFEC = tx.EffectiveEntryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            OFMONDEB = isDebit ? tx.Amount : 0m,
            OFMONCRE = isDebit ? 0m : tx.Amount,
            OFIDARCH = tx.AchBatchId,
            OFIDLOT = tx.AchBatchId,
            OFST = "PENDIENTE",
            OFIDTX = !string.IsNullOrWhiteSpace(tx.TransactionExternalId)
                ? tx.TransactionExternalId
                : tx.Reference ?? tx.Id.ToString(CultureInfo.InvariantCulture),
            OFIDREVER = 0,
            OFIDEBAPLI = tx.Id,
            OFIDCAMCOMPE = cycle.ClearingHouseId,
            OFDIRECCIONIP = "0.0.0.0",
            OFLIBRE = executionDateTime.ToString("O", CultureInfo.InvariantCulture),
            OFLIBRE1 = 0,
            ANSIDLOTE = 0,
            ANSST = string.Empty,
            ANCLC = string.Empty,
            ANSIDTX = string.Empty,
            ANSIDREVER = 0
        };
    }

    private static void ValidateRequestContract(ProcContrapartidasRequestContract request)
    {
        if (string.IsNullOrWhiteSpace(request.OFNIT)) throw new InvalidOperationException("OFNIT es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.OFEMP)) throw new InvalidOperationException("OFEMP es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.OFCTA)) throw new InvalidOperationException("OFCTA es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.OFIDTX)) throw new InvalidOperationException("OFIDTX es obligatorio.");
        if (request.OFIDLOT <= 0) throw new InvalidOperationException("OFIDLOT debe ser mayor a cero.");
    }
}
