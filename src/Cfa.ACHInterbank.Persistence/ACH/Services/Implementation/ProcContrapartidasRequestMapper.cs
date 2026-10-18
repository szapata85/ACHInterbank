using System.Globalization;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ProcContrapartidasRequestMapper : IProcContrapartidasRequestMapper
{
    private const string MethodCode = "WSCFAACH.Proc_Contrapartidas";
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly IProcContrapartidasFunctionalMappingResolver _functionalResolver;
    private readonly ProcContrapartidasDispatchOptions _options;
    private readonly IIntegrationRequestResolver? _requestResolver;
    private readonly ISoapRequestBodyWriter? _soapBodyWriter;
    private readonly ProcContrapartidasRequestParityPolicy _parityPolicy;

    public ProcContrapartidasRequestMapper(
        IProcContrapartidasFunctionalMappingResolver functionalResolver,
        IOptions<ProcContrapartidasDispatchOptions>? options = null,
        IIntegrationRequestResolver? requestResolver = null,
        ISoapRequestBodyWriter? soapBodyWriter = null,
        ProcContrapartidasRequestParityPolicy? parityPolicy = null)
    {
        _functionalResolver = functionalResolver ?? throw new ArgumentNullException(nameof(functionalResolver));
        _options = options?.Value ?? new ProcContrapartidasDispatchOptions();
        _requestResolver = requestResolver;
        _soapBodyWriter = soapBodyWriter;
        _parityPolicy = parityPolicy ?? new ProcContrapartidasRequestParityPolicy();
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

        return _options.UseContractDrivenRequest
            ? await ResolveContractDrivenAsync(cycle, transactions, executionDateTime, ct)
            : await ResolveLegacyAsync(cycle, transactions, executionDateTime, ct);
    }

    private async Task<ProcContrapartidasRequestResolution> ResolveLegacyAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct)
    {
        var configured = await _functionalResolver.TryResolveAsync(cycle, transactions, executionDateTime, ct);
        if (configured is not null)
        {
            return configured;
        }

        throw new InvalidOperationException(
            "INTEGRATION_MAPPING_REQUIRED: Proc_Contrapartidas requiere IntegrationMappingSet publicado con mappings requeridos activos. No se permite fallback transicional para construir XML.");
    }

    private async Task<ProcContrapartidasRequestResolution> ResolveContractDrivenAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct)
    {
        // Execute the current typed resolver first so its functional/mapping/conversion guards
        // remain an explicit compatibility gate during the feature-flagged cutover.
        var legacyResolution = await ResolveLegacyAsync(cycle, transactions, executionDateTime, ct);

        if (_requestResolver is null)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_CONTRACT_ENGINE_NOT_CONFIGURED: IIntegrationRequestResolver no está registrado.");
        }

        if (_soapBodyWriter is null)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_CONTRACT_ENGINE_NOT_CONFIGURED: ISoapRequestBodyWriter no está registrado.");
        }

        var primaryTransaction = transactions.OrderBy(transaction => transaction.Id).First();
        var executionDateUtc = executionDateTime.Kind == DateTimeKind.Utc
            ? executionDateTime
            : executionDateTime.ToUniversalTime();
        var correlationId = $"proc-contrapartidas:{cycle.Id}:{primaryTransaction.Id.ToString(CultureInfo.InvariantCulture)}";
        var executionContext = new IntegrationExecutionContext(
            MethodCode,
            correlationId,
            executionDateUtc,
            primaryTransaction.Id,
            cycle.Id);

        var genericRequest = await _requestResolver.ResolveAsync(executionContext, ct);
        EnsureSamePublishedMapping(legacyResolution, genericRequest);
        var parityRequest = _parityPolicy.Apply(genericRequest, legacyResolution.Contract);

        return new ProcContrapartidasRequestResolution
        {
            Contract = legacyResolution.Contract with { ContractDrivenRequest = parityRequest },
            MappingSetId = genericRequest.MappingSetId,
            MappingVersion = genericRequest.MappingVersion,
            MappingSnapshotHash = legacyResolution.MappingSnapshotHash,
            UsedFallback = legacyResolution.UsedFallback
        };
    }

    public string BuildSoapBody(ProcContrapartidasRequestContract request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.UseContractDrivenRequest)
        {
            var resolved = request.ContractDrivenRequest
                ?? throw new InvalidOperationException(
                    "PROC_CONTRAPARTIDAS_CONTRACT_REQUEST_MISSING: la solicitud no contiene el snapshot resuelto del motor genérico.");
            return (_soapBodyWriter
                ?? throw new InvalidOperationException(
                    "PROC_CONTRAPARTIDAS_CONTRACT_ENGINE_NOT_CONFIGURED: ISoapRequestBodyWriter no está registrado."))
                .Write(resolved);
        }

        return BuildLegacySoapBody(request);
    }

    private static string BuildLegacySoapBody(ProcContrapartidasRequestContract request)
    {
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

    private static void EnsureSamePublishedMapping(
        ProcContrapartidasRequestResolution legacyResolution,
        ResolvedIntegrationRequest genericRequest)
    {
        if (legacyResolution.MappingSetId.HasValue
            && legacyResolution.MappingSetId.Value != genericRequest.MappingSetId)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_MAPPING_PARITY_MISMATCH: el motor común resolvió un MappingSet distinto al validador legacy.");
        }

        if (legacyResolution.MappingVersion.HasValue
            && legacyResolution.MappingVersion.Value != genericRequest.MappingVersion)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_MAPPING_PARITY_MISMATCH: el motor común resolvió una versión de mapping distinta al validador legacy.");
        }
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
