using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameCorrelationService : IExternalFileNameCorrelationService
{
    private readonly INachaFileIdentifierMapService _identifierMapService;

    public ExternalFileNameCorrelationService(INachaFileIdentifierMapService identifierMapService)
    {
        _identifierMapService = identifierMapService;
    }

    public async Task<ExternalFileNameCorrelationEvidence> CorrelateAsync(ExternalFileNameContext context, ExternalFileNameComponents components, CancellationToken ct = default)
    {
        var headerId = ExternalFileNameSupport.TryExtractRecord1FileIdModifier(context.NachaContent);

        bool? matchR1 = null;
        if ((ExternalFileNameSupport.IsAch(context) || ExternalFileNameSupport.IsReturnOut(context)) && components.ExternalSequence.HasValue)
        {
            var expectedId = await _identifierMapService.ResolveIdentifierAsync(components.ExternalSequence.Value, ct);
            matchR1 = headerId.HasValue && headerId.Value == expectedId;
        }

        var actualDetailCount = context.ActualDetailCount ?? ExternalFileNameSupport.CountDetailRecords(context.NachaContent);
        bool? countMatch = null;
        if (ExternalFileNameSupport.IsStaReject(context))
        {
            var declared = context.DeclaredDetailCount ?? components.DeclaredDetailCount;
            countMatch = declared.HasValue && declared.Value == actualDetailCount;
        }

        return new ExternalFileNameCorrelationEvidence
        {
            NameMatchesRecord1Identifier = matchR1,
            NameMatchesDeclaredCount = countMatch,
            HeaderFileIdModifier = headerId,
            ParsedSequence = components.ExternalSequence,
            DeclaredDetailCount = context.DeclaredDetailCount ?? components.DeclaredDetailCount,
            ActualDetailCount = actualDetailCount,
            Notes = "Correlación normativa fase 1 (bloqueo parcial controlado)."
        };
    }
}
