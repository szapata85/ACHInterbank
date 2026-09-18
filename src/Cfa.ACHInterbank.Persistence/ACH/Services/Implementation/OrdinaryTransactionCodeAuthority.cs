using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class OrdinaryTransactionCodeAuthority : IOrdinaryTransactionCodeAuthority
{
    private static readonly IReadOnlyCollection<string> Type6Record = ["6"];
    private readonly INachaConfigResolver _configResolver;
    private readonly Dictionary<AuthorityKey, Task<NachaConfigResolutionResult>> _operationCache = [];

    public OrdinaryTransactionCodeAuthority(INachaConfigResolver configResolver)
    {
        _configResolver = configResolver;
    }

    public async Task<string> ResolveAsync(
        OrdinaryTransactionCodeAuthorityContext context,
        OrdinaryTransactionCodeSemanticRequest semantic,
        CancellationToken ct = default)
    {
        var direction = semantic.Type switch
        {
            TransactionTypeEnum.Credit => NachaEntryDirection.Credit,
            TransactionTypeEnum.Debit => NachaEntryDirection.Debit,
            _ => throw new InvalidOperationException(
                "ORDINARY_TXCODE_ORIGINAL_DIRECTION_REQUIRED: la autoridad de registro requiere Credit o Debit antes de persistir una prenotificación.")
        };
        var flows = semantic.IsPrenotification
            ? new[] { "ORIGINAL", "PRENOTIFICACION" }
            : ["ORIGINAL"];
        var resolvedCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var flow in flows)
        {
            var authorities = await ResolveReachableAuthoritiesAsync(context, semantic, flow, ct);
            foreach (var (finalService, resolution) in authorities)
            {
                EnsureSelectedAuthority(resolution, context, flow, finalService);

                var contract = resolution.TransactionCodeContract
                    ?? throw new InvalidOperationException(
                        $"ORDINARY_TXCODE_CONTRACT_MISSING: {context.ClearingHouseCode}/{flow}/SALIDA/{finalService}.");
                if (!contract.TryGetRule(direction, semantic.AccountType, semantic.IsPrenotification, out var rule))
                {
                    throw new InvalidOperationException(
                        $"ORDINARY_TXCODE_TUPLE_MISSING: {context.ClearingHouseCode}/{flow}/SALIDA/{finalService}/{direction}/{semantic.AccountType}/{semantic.IsPrenotification}.");
                }

                resolvedCodes.Add(rule.TransactionCode);
            }
        }

        if (resolvedCodes.Count != 1)
        {
            throw new InvalidOperationException(
                $"ORDINARY_TXCODE_AUTHORITY_CONFLICT: las publicaciones alcanzables resolvieron {resolvedCodes.Count} códigos distintos.");
        }

        return resolvedCodes.Single();
    }

    private async Task<IReadOnlyList<(string Service, NachaConfigResolutionResult Resolution)>> ResolveReachableAuthoritiesAsync(
        OrdinaryTransactionCodeAuthorityContext context,
        OrdinaryTransactionCodeSemanticRequest semantic,
        string flow,
        CancellationToken ct)
    {
        if (!string.Equals(context.ClearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            var reachableServices = AchBatchServiceClassPolicy.ResolveReachableOrdinary(
                semantic.Type,
                semantic.IsPrenotification);
            var authorities = new List<(string Service, NachaConfigResolutionResult Resolution)>(reachableServices.Count);
            foreach (var service in reachableServices)
            {
                authorities.Add((service, await ResolvePublicationAsync(context, flow, service, requireOutboundPolicy: false, ct)));
            }

            return authorities;
        }

        var sourceService = Required(context.SourceServiceClassCode, "ORDINARY_TXCODE_SOURCE_SERVICE_REQUIRED");
        var sourceAuthority = await ResolvePublicationAsync(context, flow, sourceService, requireOutboundPolicy: true, ct);
        EnsureSelectedAuthority(sourceAuthority, context, flow, sourceService);
        var policy = sourceAuthority.OutboundPolicy
            ?? throw new InvalidOperationException(
                $"ORDINARY_TXCODE_OUTBOUND_POLICY_MISSING: {context.ClearingHouseCode}/{flow}/SALIDA/{sourceService}.");
        if (!policy.Services.Any(service => string.Equals(service.ServiceCode, sourceService, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"ORDINARY_TXCODE_SOURCE_SERVICE_UNREACHABLE: el servicio {sourceService} no participa en la política publicada {policy.ProfileCode}.");
        }

        var finalService = policy.FileAllocation == NachaOutboundFileAllocation.CombineServicePartitionsByIndex
            ? policy.Services.OrderBy(service => service.Order).Select(service => service.ServiceCode).First()
            : sourceService;
        return
        [
            string.Equals(finalService, sourceService, StringComparison.OrdinalIgnoreCase)
                ? (finalService, sourceAuthority)
                : (finalService, await ResolvePublicationAsync(context, flow, finalService, requireOutboundPolicy: false, ct))
        ];
    }

    private Task<NachaConfigResolutionResult> ResolvePublicationAsync(
        OrdinaryTransactionCodeAuthorityContext context,
        string flow,
        string service,
        bool requireOutboundPolicy,
        CancellationToken ct)
    {
        var key = new AuthorityKey(
            context.ClearingHouseCode.Trim().ToUpperInvariant(),
            flow,
            service.Trim().ToUpperInvariant(),
            context.ProcessDate.Date,
            context.ClearingHouseId,
            context.CycleName.Trim(),
            requireOutboundPolicy);
        if (_operationCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var pending = _configResolver.ResolvePublishedOrdinaryAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = key.ClearingHouseCode,
            FlowTypeCode = key.Flow,
            DirectionCode = "SALIDA",
            ServiceClassCode = key.Service,
            ProcessDateUtc = key.ProcessDate,
            RequireOutboundPolicy = requireOutboundPolicy,
            RecordCodes = Type6Record,
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CycleName"] = key.CycleName,
                ["ClearingHouseId"] = key.ClearingHouseId.ToString(CultureInfo.InvariantCulture)
            }
        }, ct);
        _operationCache.Add(key, pending);
        return pending;
    }

    private static void EnsureSelectedAuthority(
        NachaConfigResolutionResult resolution,
        OrdinaryTransactionCodeAuthorityContext context,
        string flow,
        string service)
    {
        if (!resolution.Success || resolution.Profile is null || resolution.UsedFallback)
        {
            throw new InvalidOperationException(
                $"ORDINARY_TXCODE_AUTHORITY_UNAVAILABLE: {context.ClearingHouseCode}/{flow}/SALIDA/{service}; Status={resolution.SelectionStatus}.");
        }
    }

    private static string Required(string value, string code)
        => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{code}: no se recibió la dimensión de servicio requerida.")
            : value.Trim().ToUpperInvariant();

    private sealed record AuthorityKey(
        string ClearingHouseCode,
        string Flow,
        string Service,
        DateTime ProcessDate,
        int ClearingHouseId,
        string CycleName,
        bool RequireOutboundPolicy);
}
