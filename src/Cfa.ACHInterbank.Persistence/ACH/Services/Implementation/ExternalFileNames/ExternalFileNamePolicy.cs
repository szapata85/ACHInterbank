using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNamePolicy : IExternalFileNamePolicy
{
    private readonly IExternalFileNameBuilder _builder;
    private readonly IExternalFileNameValidator _validator;
    private readonly IExternalFileNameCorrelationService _correlation;
    private readonly IExternalFileNameAuditService _audit;
    private readonly IExternalFileDuplicateGuard _duplicateGuard;

    public ExternalFileNamePolicy(
        IExternalFileNameBuilder builder,
        IExternalFileNameValidator validator,
        IExternalFileNameCorrelationService correlation,
        IExternalFileNameAuditService audit,
        IExternalFileDuplicateGuard duplicateGuard)
    {
        _builder = builder;
        _validator = validator;
        _correlation = correlation;
        _audit = audit;
        _duplicateGuard = duplicateGuard;
    }

    public async Task<ExternalFileNamePolicyResult> GenerateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var components = await _builder.BuildAsync(context, ct);
        var validationContext = NormalizeRecord1FileIdForValidation(context, components);
        var validation = await _validator.ValidateAsync(validationContext, components, ct);
        var correlation = await _correlation.CorrelateAsync(validationContext, components, ct);
        var result = new ExternalFileNamePolicyResult
        {
            ExternalFileName = components.FullName,
            Components = components,
            Validation = validation,
            CorrelationEvidence = correlation
        };

        await _audit.RegisterAsync(validationContext, result, ct);
        return result;
    }

    public async Task<ExternalFileNameValidationResult> ValidateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var components = await _builder.BuildAsync(context, ct);
        return await _validator.ValidateAsync(context, components, ct);
    }

    public async Task<ExternalFileNameCorrelationEvidence> CorrelateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var components = await _builder.BuildAsync(context, ct);
        return await _correlation.CorrelateAsync(context, components, ct);
    }

    public async Task RegisterExternalNameAsync(ExternalFileNameContext context, ExternalFileNamePolicyResult result, CancellationToken ct = default)
    {
        await _audit.RegisterAsync(context, result, ct);
    }

    public async Task<bool> CheckDuplicateAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var components = await _builder.BuildAsync(context, ct);
        return await _duplicateGuard.IsDuplicateAsync(context, components.FullName, ct);
    }

    public async Task<ExternalFileNameComponents> PreviewExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        return await _builder.BuildAsync(context, ct);
    }

    private static ExternalFileNameContext NormalizeRecord1FileIdForValidation(ExternalFileNameContext context, ExternalFileNameComponents components)
    {
        if (!components.FileIdModifier.HasValue || string.IsNullOrWhiteSpace(context.NachaContent))
        {
            return context;
        }

        return new ExternalFileNameContext
        {
            ClearingHouseId = context.ClearingHouseId,
            ClearingHouseCode = context.ClearingHouseCode,
            ClearingHouseOriginCode = components.Prefix ?? context.ClearingHouseOriginCode,
            CycleId = context.CycleId,
            CycleName = context.CycleName,
            ProcessingDate = context.ProcessingDate,
            ExternalFileType = context.ExternalFileType,
            Flow = context.Flow,
            Direction = context.Direction,
            IsPse = context.IsPse,
            ProvidedExternalFileName = context.ProvidedExternalFileName,
            InternalFileName = context.InternalFileName,
            NachaContent = ExternalFileNameSupport.ReplaceRecord1FileIdModifier(context.NachaContent, components.FileIdModifier.Value),
            DeclaredDetailCount = context.DeclaredDetailCount,
            ActualDetailCount = context.ActualDetailCount,
            FileHash = context.FileHash,
            FileSize = context.FileSize,
            RequestedBy = context.RequestedBy
        };
    }
}
