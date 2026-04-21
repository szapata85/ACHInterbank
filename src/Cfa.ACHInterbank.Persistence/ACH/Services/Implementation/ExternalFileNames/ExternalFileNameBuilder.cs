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

    public ExternalFileNameBuilder(IExternalFileNameSequenceService sequenceService, INachaFileIdentifierMapService identifierMapService)
    {
        _sequenceService = sequenceService;
        _identifierMapService = identifierMapService;
    }

    public async Task<ExternalFileNameComponents> BuildAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(context.ProvidedExternalFileName))
        {
            return ExternalFileNameSupport.Parse(context, context.ProvidedExternalFileName.Trim());
        }

        if (ExternalFileNameSupport.IsAch(context))
        {
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            var externalName = ExternalFileNameSupport.BuildAchName(context.ClearingHouseOriginCode ?? string.Empty, sequence);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = context.ClearingHouseOriginCode,
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
}
