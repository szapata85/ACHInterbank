using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameBuilder : IExternalFileNameBuilder
{
    private readonly IExternalFileNameSequenceService _sequenceService;
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly INachaFileNamingRuleService? _namingRuleService;

    public ExternalFileNameBuilder(
        IExternalFileNameSequenceService sequenceService,
        INachaFileIdentifierMapService identifierMapService,
        INachaFileNamingRuleService? namingRuleService = null)
    {
        _sequenceService = sequenceService;
        _identifierMapService = identifierMapService;
        _namingRuleService = namingRuleService;
    }

    public async Task<ExternalFileNameComponents> BuildAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(context.ProvidedExternalFileName))
        {
            return ExternalFileNameSupport.Parse(context, context.ProvidedExternalFileName.Trim());
        }

        if (ExternalFileNameSupport.IsAch(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var externalName = BuildConfiguredName(namingRule?.NamePattern, originCode, sequence);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                FileIdModifier = fileId
            };
        }

        if (ExternalFileNameSupport.IsReturnOut(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var externalName = ExternalFileNameSupport.BuildReturnName(originCode, sequence);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                FileIdModifier = fileId
            };
        }

        if (ExternalFileNameSupport.IsStaReject(context))
        {
            var declared = context.DeclaredDetailCount ?? context.ActualDetailCount ?? ExternalFileNameSupport.CountDetailRecords(context.NachaContent);
            var name = $"STA.REJECT.{declared:D6}.txt";
            return new ExternalFileNameComponents { FullName = name, DeclaredDetailCount = declared };
        }

        return new ExternalFileNameComponents
        {
            FullName = context.InternalFileName ?? $"AUDIT_{DateTime.UtcNow:yyyyMMddHHmmss}.txt"
        };
    }

    private static string BuildConfiguredName(string? namePattern, string originCode, int sequence)
    {
        var defaultName = ExternalFileNameSupport.BuildAchName(originCode, sequence);
        if (string.IsNullOrWhiteSpace(namePattern) || string.Equals(namePattern, "RRRRTTT.ZZZ.1", StringComparison.OrdinalIgnoreCase))
        {
            return defaultName;
        }

        return namePattern
            .Replace("RRRRTTT", originCode[^7..], StringComparison.OrdinalIgnoreCase)
            .Replace("ZZZ", sequence.ToString("D3"), StringComparison.OrdinalIgnoreCase);
    }
}
