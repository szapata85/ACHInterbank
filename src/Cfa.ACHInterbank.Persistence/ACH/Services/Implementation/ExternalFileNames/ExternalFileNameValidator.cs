using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameValidator : IExternalFileNameValidator
{
    private readonly IExternalFileDuplicateGuard _duplicateGuard;
    private readonly IExternalFileNameCorrelationService _correlationService;
    private readonly INachaFileIdentifierMapService _identifierMapService;

    public ExternalFileNameValidator(
        IExternalFileDuplicateGuard duplicateGuard,
        IExternalFileNameCorrelationService correlationService,
        INachaFileIdentifierMapService identifierMapService)
    {
        _duplicateGuard = duplicateGuard;
        _correlationService = correlationService;
        _identifierMapService = identifierMapService;
    }

    public async Task<ExternalFileNameValidationResult> ValidateAsync(ExternalFileNameContext context, ExternalFileNameComponents components, CancellationToken ct = default)
    {
        var issues = new List<ExternalFileNameValidationIssue>();

        if (ExternalFileNameSupport.IsReturnOut(context))
        {
            var parsed = ExternalFileNameSupport.Parse(context, components.FullName);
            if (!parsed.ExternalSequence.HasValue)
            {
                issues.Add(Hard(
                    "RETURN_NAME_PATTERN",
                    "RETURN_PATTERN_INVALID",
                    "Regla HARD BLOCK RET: patron requerido RRRRTTT.ZZZ.RET.",
                    "ACH V32 6.1.10.1 / RET"));
            }
            else
            {
                if (parsed.ExternalSequence.Value is < 1 or > 36)
                {
                    issues.Add(Hard(
                        "RETURN_DAILY_LIMIT",
                        "RETURN_SEQUENCE_RANGE",
                        "Regla HARD BLOCK RET: secuencia ZZZ debe estar entre 001 y 036.",
                        "ACH V32 6.1.10.1 / RET"));
                }
                else
                {
                    var expected = await _identifierMapService.ResolveIdentifierAsync(parsed.ExternalSequence.Value, ct);
                    var correlation = await _correlationService.CorrelateAsync(context, parsed, ct);
                    if (correlation.HeaderFileIdModifier.HasValue && correlation.HeaderFileIdModifier.Value != expected)
                    {
                        issues.Add(Hard(
                            "RETURN_ZZZ_R1",
                            "RETURN_IDENTIFIER_MISMATCH",
                            "Regla HARD BLOCK RET: ZZZ no corresponde al campo 7 del Registro 1.",
                            "ACH V32 6.1.10.1 / RET"));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(components.FullName))
            {
                var duplicated = await _duplicateGuard.IsDuplicateAsync(context, components.FullName, ct);
                if (duplicated)
                {
                    issues.Add(Hard(
                        "RETURN_DUPLICATE_NAME",
                        "RETURN_DUPLICATE",
                        "Regla HARD BLOCK RET: archivo duplicado.",
                        "ACH V32 6.1.10.1 / RET"));
                }
            }
        }
        else if (ExternalFileNameSupport.IsUnconfirmedReturnLikeOutFlow(context))
        {
            issues.Add(Warning(
                "RETURN_NAMING_PROVISIONAL",
                "RETURN_NORMATIVE_PENDING",
                "Regla WARNING: flujo de devolucion/ROR/rechazo-respuesta saliente en modo provisional UAT, sin hard-block normativo.",
                "Matriz vigente naming externo ACH/CENIT/STA"));

            if (!string.IsNullOrWhiteSpace(components.FullName))
            {
                var duplicated = await _duplicateGuard.IsDuplicateAsync(context, components.FullName, ct);
                if (duplicated)
                {
                    issues.Add(Warning(
                        "RETURN_DUPLICATE_NAME",
                        "RETURN_DUPLICATE_WARNING",
                        "Regla WARNING: duplicidad detectada en flujo provisional de devoluciones salientes.",
                        "Matriz vigente naming externo ACH/CENIT/STA"));
                }
            }
        }
        else if (ExternalFileNameSupport.IsCenitNachaOut(context))
        {
            if (!CenitOfficialFileNameParser.TryParseCenitFileName(components.FullName, out var parsed))
            {
                issues.Add(Hard(
                    "CENIT_NAME_PATTERN",
                    "CENIT_PATTERN_INVALID",
                    "Regla HARD BLOCK CENIT: patrón requerido RRRRTTT.CCC.YYYYMMDD.S sin extensión.",
                    "CENIT-DSP-152-Anexo-2 / convención operativa vigente"));
            }
            else
            {
                var expectedOrigin = components.Prefix ?? context.ClearingHouseOriginCode;
                if (!string.IsNullOrWhiteSpace(expectedOrigin)
                    && !string.Equals(expectedOrigin[^Math.Min(7, expectedOrigin.Length)..], parsed!.OriginCode, StringComparison.Ordinal))
                {
                    issues.Add(Hard("CENIT_ORIGIN_MISMATCH", "CENIT_ORIGIN_MISMATCH", "El código de origen del nombre CENIT no coincide con la entidad configurada.", "CENIT-DSP-152-Anexo-2"));
                }
                else if (context.CycleNumber is > 0 && context.CycleNumber.Value != parsed!.CycleNumber)
                {
                    issues.Add(Hard("CENIT_CYCLE_MISMATCH", "CENIT_CYCLE_MISMATCH", "El ciclo del nombre CENIT no coincide con el ciclo operativo.", "CENIT-DSP-152-Anexo-2"));
                }
                else if (DateOnly.FromDateTime(context.ProcessingDate) != parsed!.FileDate)
                {
                    issues.Add(Hard("CENIT_DATE_MISMATCH", "CENIT_DATE_MISMATCH", "La fecha del nombre CENIT no coincide con la fecha operativa.", "CENIT-DSP-152-Anexo-2"));
                }
            }
        }
        else if (ExternalFileNameSupport.IsAch(context))
        {
            var parsed = ExternalFileNameSupport.Parse(context, components.FullName);
            if (!parsed.ExternalSequence.HasValue)
            {
                issues.Add(Hard("ACH_NAME_PATTERN", "ACH_PATTERN_INVALID", "Regla HARD BLOCK ACH: patron requerido RRRRTTT.ZZZ.N.", "ACH V32 6.1.10.1"));
            }
            else
            {
                if (parsed.ExternalSequence.Value is < 1 or > 36)
                {
                    issues.Add(Hard("ACH_DAILY_LIMIT", "ACH_SEQUENCE_RANGE", "Regla HARD BLOCK ACH: secuencia ZZZ debe estar entre 001 y 036.", "ACH V32 6.1.10.1 / 6.1.10.3"));
                }
                else if (!parsed.CycleNumber.HasValue || parsed.CycleNumber.Value < 1)
                {
                    issues.Add(Hard("ACH_CYCLE_PATTERN", "ACH_CYCLE_INVALID", "Regla HARD BLOCK ACH: el ciclo N debe ser un entero positivo.", "ACH V32 6.1.10.1"));
                }
                else if (context.CycleNumber is > 0 && context.CycleNumber.Value != parsed.CycleNumber.Value)
                {
                    issues.Add(Hard("ACH_CYCLE_MISMATCH", "ACH_CYCLE_MISMATCH", "Regla HARD BLOCK ACH: el ciclo del nombre no coincide con el ciclo operativo resuelto.", "ACH V32 6.1.10.1"));
                }
                else
                {
                    var expected = await _identifierMapService.ResolveIdentifierAsync(parsed.ExternalSequence.Value, ct);
                    var correlation = await _correlationService.CorrelateAsync(context, parsed, ct);
                    if (correlation.HeaderFileIdModifier.HasValue && correlation.HeaderFileIdModifier.Value != expected)
                    {
                        issues.Add(Hard("ACH_ZZZ_R1", "ACH_IDENTIFIER_MISMATCH", "Regla HARD BLOCK ACH: ZZZ no corresponde al campo 7 del Registro 1.", "ACH V32 6.1.10.1 / causal 14"));
                    }
                }
            }

            if (context.IsPse)
            {
                var correlation = await _correlationService.CorrelateAsync(context, parsed, ct);
                if (!correlation.HeaderFileIdModifier.HasValue || correlation.HeaderFileIdModifier.Value is < '4' or > '9')
                {
                    issues.Add(Hard("ACH_PSE_RANGE", "PSE_IDENTIFIER_RANGE", "Regla HARD BLOCK PSE: campo 7 debe estar en rango 4..9 cuando aplique PSE.", "ACH V32 6.1.10.2"));
                }
                else
                {
                    issues.Add(Warning("ACH_PSE_DETAILS", "PSE_DETAILS_PENDING", "Regla WARNING: reglas PSE completas permanecen delegadas al Manual de Operaciones PSE.", "ACH V32 6.1.10.2/6.1.10.4"));
                }
            }
            else
            {
                var duplicated = await _duplicateGuard.IsDuplicateAsync(context, components.FullName, ct);
                if (duplicated)
                {
                    issues.Add(Warning("ACH_DUPLICATE_NAME", "ACH_DUPLICATE_WARNING", "Regla WARNING: duplicidad por nombre ACH fuera de D31 lote no se bloquea en fase 1.", "Matriz v2"));
                }
            }
        }
        else if (ExternalFileNameSupport.IsStaReject(context))
        {
            var actualDetailCount = context.ActualDetailCount ?? ExternalFileNameSupport.CountDetailRecords(context.NachaContent);
            var declared = context.DeclaredDetailCount ?? components.DeclaredDetailCount;

            if (!declared.HasValue)
            {
                issues.Add(Hard("STA_FIELD6_REQUIRED", "STA_DECLARED_COUNT_MISSING", "Regla HARD BLOCK STA rechazo: campo 6 (numero de registros de detalle) es obligatorio.", "CENIT Anexo 2 Cap.2 num.4"));
            }
            else if (declared.Value != actualDetailCount)
            {
                issues.Add(Hard("STA_D05", "STA_COUNT_MISMATCH", "Regla HARD BLOCK STA rechazo: D05 mismatch entre nombre externo y contenido.", "CENIT Anexo B D05"));
            }

            var duplicate = await _duplicateGuard.IsDuplicateAsync(context, components.FullName, ct);
            if (duplicate)
            {
                issues.Add(Hard("STA_D04", "STA_DUPLICATE", "Regla HARD BLOCK STA rechazo: D04 archivo duplicado.", "CENIT Anexo B D04"));
            }
        }
        else if (ExternalFileNameSupport.IsCenit(context))
        {
            issues.Add(Warning("STA_FULL_NAMING", "STA_NAMING_PARTIAL", "Regla WARNING: naming STA fuera de rechazo no se bloquea en fase 1.", "CENIT Anexo 2"));
        }
        else
        {
            issues.Add(Audit("UNMAPPED_CHAMBER", "AUDIT_NORMATIVE_CONFIRMATION", "Regla AUDIT ONLY: camara/flujos no cerrados normativamente para enforcement duro.", "Matriz v2"));
        }

        var disposition = ResolveDisposition(issues);
        return new ExternalFileNameValidationResult { Disposition = disposition, Issues = issues };
    }

    private static ExternalFileValidationDisposition ResolveDisposition(IReadOnlyCollection<ExternalFileNameValidationIssue> issues)
    {
        if (issues.Any(x => x.Disposition == ExternalFileValidationDisposition.HardBlock))
        {
            return ExternalFileValidationDisposition.HardBlock;
        }

        if (issues.Any(x => x.Disposition == ExternalFileValidationDisposition.Warning))
        {
            return ExternalFileValidationDisposition.Warning;
        }

        if (issues.Any(x => x.Disposition == ExternalFileValidationDisposition.AuditOnly))
        {
            return ExternalFileValidationDisposition.AuditOnly;
        }

        return ExternalFileValidationDisposition.Passed;
    }

    private static ExternalFileNameValidationIssue Hard(string rule, string code, string message, string source)
        => new() { RuleCode = rule, IssueCode = code, Message = message, SourceReference = source, Disposition = ExternalFileValidationDisposition.HardBlock };

    private static ExternalFileNameValidationIssue Warning(string rule, string code, string message, string source)
        => new() { RuleCode = rule, IssueCode = code, Message = message, SourceReference = source, Disposition = ExternalFileValidationDisposition.Warning };

    private static ExternalFileNameValidationIssue Audit(string rule, string code, string message, string source)
        => new() { RuleCode = rule, IssueCode = code, Message = message, SourceReference = source, Disposition = ExternalFileValidationDisposition.AuditOnly };
}
