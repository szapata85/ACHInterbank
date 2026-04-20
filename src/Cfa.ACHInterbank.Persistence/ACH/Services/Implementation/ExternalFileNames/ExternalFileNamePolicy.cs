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
        var validation = await _validator.ValidateAsync(context, components, ct);
        var correlation = await _correlation.CorrelateAsync(context, components, ct);
        var result = new ExternalFileNamePolicyResult
        {
            ExternalFileName = components.FullName,
            Components = components,
            Validation = validation,
            CorrelationEvidence = correlation
        };

        await _audit.RegisterAsync(context, result, ct);
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
}
