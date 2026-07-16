using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
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

        if (ExternalFileNameSupport.IsCenitNachaOut(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var cycleNumber = ResolveCycleNumber(context);
            var externalName = ExternalFileNameSupport.BuildCenitName(
                originCode,
                cycleNumber,
                context.ProcessingDate,
                sequence);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                CycleNumber = cycleNumber
            };
        }

        if (ExternalFileNameSupport.IsAch(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var cycleNumber = ResolveCycleNumber(context);
            var externalName = BuildConfiguredName(namingRule?.NamePattern, originCode, sequence, cycleNumber);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                CycleNumber = cycleNumber,
                FileIdModifier = fileId
            };
        }

        if (ExternalFileNameSupport.IsReturnOut(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var originCode = namingRule?.OriginEntityCode ?? string.Empty;
            if (string.IsNullOrWhiteSpace(originCode))
            {
                throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
            }
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

    private static string BuildConfiguredName(string? namePattern, string originCode, int sequence, int cycleNumber)
    {
        var defaultName = ExternalFileNameSupport.BuildAchName(originCode, sequence, cycleNumber);
        if (string.IsNullOrWhiteSpace(namePattern) || Regex.IsMatch(namePattern, @"^RRRRTTT\.ZZZ\.(?:N|[1-9]\d*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return defaultName;
        }

        return namePattern
            .Replace("RRRRTTT", originCode[^7..], StringComparison.OrdinalIgnoreCase)
            .Replace("ZZZ", sequence.ToString("D3"), StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveCycleNumber(ExternalFileNameContext context)
    {
        if (context.CycleNumber is > 0)
        {
            return context.CycleNumber.Value;
        }

        if (ExternalFileNameSupport.TryExtractPositiveCycleNumber(context.CycleName, out var cycleNumber))
        {
            return cycleNumber;
        }

        throw new InvalidOperationException("No se pudo resolver un numero de ciclo positivo unico desde CycleName para la generacion NACHA-M outbound.");
    }
}
