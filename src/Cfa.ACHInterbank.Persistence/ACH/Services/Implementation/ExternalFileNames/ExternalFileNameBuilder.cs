using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameBuilder : IExternalFileNameBuilder
{
    private readonly IExternalFileNameSequenceService _sequenceService;
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly INachaFileNamingRuleService? _namingRuleService;
    private readonly IExternalFileNameReservationService? _reservationService;
    private readonly NachaGenerationOptions _generationOptions;

    public ExternalFileNameBuilder(
        IExternalFileNameSequenceService sequenceService,
        INachaFileIdentifierMapService identifierMapService,
        INachaFileNamingRuleService? namingRuleService = null,
        IExternalFileNameReservationService? reservationService = null,
        IOptions<NachaGenerationOptions>? generationOptions = null)
    {
        _sequenceService = sequenceService;
        _identifierMapService = identifierMapService;
        _namingRuleService = namingRuleService;
        _reservationService = reservationService;
        _generationOptions = generationOptions?.Value ?? new NachaGenerationOptions { ExecutionScope = "DEVELOPMENT" };
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
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var cycleNumber = ResolveCycleNumber(context);
            var reservation = await ReserveSequenceAsync(context, originCode, cycleNumber, namingRule?.NamePattern, ct);
            var sequence = reservation.Sequence;
            var externalName = ExternalFileNameSupport.BuildCenitName(
                originCode,
                cycleNumber,
                context.ProcessingDate,
                sequence);

            await CompleteReservationAsync(reservation, externalName, null, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                CycleNumber = cycleNumber,
                ReservationId = reservation.ReservationId,
                ReusedReservation = reservation.WasReused
            };
        }

        if (ExternalFileNameSupport.IsAch(context))
        {
            EnforceAchColLiveNamingGate(context);
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var originCode = namingRule?.OriginEntityCode ?? context.ClearingHouseOriginCode ?? string.Empty;
            var cycleNumber = ResolveCycleNumber(context);
            var reservation = await ReserveSequenceAsync(context, originCode, cycleNumber, namingRule?.NamePattern, ct);
            var sequence = reservation.Sequence;
            var externalName = BuildConfiguredName(namingRule?.NamePattern, originCode, sequence, cycleNumber);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);
            await CompleteReservationAsync(reservation, externalName, fileId, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                CycleNumber = cycleNumber,
                FileIdModifier = fileId,
                ReservationId = reservation.ReservationId,
                ReusedReservation = reservation.WasReused
            };
        }

        if (ExternalFileNameSupport.IsReturnOut(context))
        {
            var namingRule = _namingRuleService is null
                ? null
                : await _namingRuleService.GetActiveOutboundRuleAsync(context.ClearingHouseId, context.ProcessingDate, ct);
            var originCode = namingRule?.OriginEntityCode ?? string.Empty;
            if (string.IsNullOrWhiteSpace(originCode))
            {
                throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
            }
            var reservation = await ReserveSequenceAsync(context, originCode, null, namingRule?.NamePattern, ct);
            var sequence = reservation.Sequence;
            var externalName = ExternalFileNameSupport.BuildReturnName(originCode, sequence);
            var fileId = await _identifierMapService.ResolveIdentifierAsync(sequence, ct);
            await CompleteReservationAsync(reservation, externalName, fileId, ct);

            return new ExternalFileNameComponents
            {
                FullName = externalName,
                Prefix = originCode,
                ExternalSequence = sequence,
                FileIdModifier = fileId,
                ReservationId = reservation.ReservationId,
                ReusedReservation = reservation.WasReused
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
            FullName = context.InternalFileName
                ?? $"AUDIT_{(context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow):yyyyMMddHHmmss}.txt"
        };
    }

    private async Task<SequenceReservation> ReserveSequenceAsync(
        ExternalFileNameContext context,
        string originCode,
        int? cycleNumber,
        string? namingPattern,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
            return new SequenceReservation(sequence, null, false);
        }

        if (_reservationService is null)
        {
            throw new InvalidOperationException("ACH_EXTERNAL_FILENAME_RESERVATION_SERVICE_REQUIRED: no existe proveedor de reservas idempotentes.");
        }

        var fingerprint = string.Join('|',
            "ACH-EXTERNAL-NAME-V1",
            context.ClearingHouseId,
            context.ProcessingDate.ToString("yyyyMMdd"),
            context.ExternalFileType,
            context.Flow,
            context.Direction,
            originCode,
            cycleNumber?.ToString() ?? "NA",
            namingPattern ?? "DEFAULT",
            ComputeContentHash(context.NachaContent));
        var reservation = await _reservationService.ReserveAsync(context, fingerprint, ct);
        return new SequenceReservation(reservation.Sequence, reservation.ReservationId, reservation.WasReused);
    }

    private async Task CompleteReservationAsync(
        SequenceReservation reservation,
        string externalFileName,
        char? fileIdModifier,
        CancellationToken ct)
    {
        if (reservation.ReservationId.HasValue)
        {
            await _reservationService!.CompleteAsync(
                reservation.ReservationId.Value,
                externalFileName,
                fileIdModifier,
                ct);
        }
    }

    private void EnforceAchColLiveNamingGate(ExternalFileNameContext context)
    {
        if (!ExternalFileNameSupport.IsAchColombiaNachaOut(context)
            || !string.Equals(_generationOptions.ExecutionScope, "LIVE", StringComparison.OrdinalIgnoreCase)
            || _generationOptions.AchColExternalNamingHomologated)
        {
            return;
        }

        throw new InvalidOperationException(
            "ACHCOL-FILENAME-CONTRACTUAL-NOT-DEMONSTRATED: el naming externo final no está homologado y permanece bloqueado para LIVE.");
    }

    private static string ComputeContentHash(string? content)
        => string.IsNullOrEmpty(content)
            ? "NO-CONTENT"
            : Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(content)));

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

    private sealed record SequenceReservation(int Sequence, long? ReservationId, bool WasReused);
}
