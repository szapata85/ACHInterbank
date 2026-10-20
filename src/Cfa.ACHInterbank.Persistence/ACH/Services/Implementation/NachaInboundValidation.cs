using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using NachaHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedNachaHeader;
using BatchHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedBatchHeader;
using EntryDetail = Cfa.ACHInterbank.Application.ACH.Models.ParsedEntryDetail;
using AddendaRecord = Cfa.ACHInterbank.Application.ACH.Models.ParsedAddendaRecord;
using BatchControl = Cfa.ACHInterbank.Application.ACH.Models.ParsedBatchControl;
using FileControl = Cfa.ACHInterbank.Application.ACH.Models.ParsedFileControl;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// R5B inbound validation boundary. Pure/inter-record/profile validation lives here; DB-backed
/// validation is isolated in <see cref="NachaInboundDatabaseValidator"/>. Neither component writes
/// persistence state or owns managed-file transport, filesystem, HTTP, dispatch or post-processing.
/// </summary>
internal sealed class NachaInboundValidator
{
    private enum ValidationBoundary
    {
        Regulatory,
        Technical
    }

    private readonly IReadOnlyDictionary<string, AchFileRejectionCode> _rejectionCatalog;

    internal NachaInboundValidator(IReadOnlyDictionary<string, AchFileRejectionCode> rejectionCatalog)
    {
        _rejectionCatalog = rejectionCatalog;
    }

    internal NachaInboundValidationContext CreateContext(
        NachaPublicationSnapshot? selectedPublication,
        NachaParseRequest? request,
        NachaProfileRecordReader? profileReader)
    {
        var publishedAuthority = selectedPublication is null
            ? null
            : ResolvePublishedInboundAuthority(selectedPublication, request!);
        var isCenitInboundProfile = publishedAuthority is not null
            ? publishedAuthority.IsCenit
            : CenitOrdinaryInbound2026Layout.IsProfile(profileReader?.ProfileCode);
        var isCenitCtxInboundProfile = publishedAuthority is not null
            ? publishedAuthority.HasT6AddendaCount
            : CenitOrdinaryInbound2026Layout.IsCtxProfile(profileReader?.ProfileCode);

        return new NachaInboundValidationContext(publishedAuthority, isCenitInboundProfile, isCenitCtxInboundProfile);
    }

    internal NachaTransactionCodeSemanticRule? ResolvePublishedTransactionRule(
        NachaInboundPublishedAuthority? authority,
        EntryDetail entry)
    {
        if (authority is null)
        {
            return null;
        }

        if (!authority.TransactionCodes.TryGetRule(entry.TransactionCode ?? string.Empty, out var rule)
            || rule.IsPrenotification != (authority.Snapshot.Profile.FlowTypeCode == "PRENOTIFICACION"))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T6_SEMANTIC_MISMATCH");
        }

        return rule;
    }

    internal NachaEntryAddendaPreparation ResolveEntryAddendaPreparation(
        NachaInboundValidationContext context,
        BatchHeader? currentBatch,
        EntryDetail entry,
        NachaTransactionCodeSemanticRule? publishedRule)
    {
        NachaAddendaBounds? publishedBounds = null;
        if (context.PublishedAuthority?.Cardinality is not null)
        {
            var serviceCode = currentBatch?.StandardEntryClassCode?.Trim() ?? string.Empty;
            if (!context.PublishedAuthority.Cardinality.TryResolve(serviceCode, publishedRule!.Direction, out publishedBounds))
            {
                throw new InvalidOperationException("CARDINALITY_POLICY_UNRESOLVED");
            }
        }

        var optionalAddenda = publishedBounds is not null
            ? publishedBounds.Minimum == 0
            : context.PublishedAuthority is null && context.IsCenitInboundProfile
              && string.Equals(currentBatch?.StandardEntryClassCode?.Trim(), "PPD", StringComparison.Ordinal)
              && NachaInboundTransactionCodeSets.PrenoteCodes.Contains(entry.TransactionCode ?? string.Empty)
              && NachaInboundTransactionCodeSets.CreditCodes.Contains(entry.TransactionCode ?? string.Empty);

        return new NachaEntryAddendaPreparation(optionalAddenda, publishedBounds);
    }

    internal int ResolveExpectedAddendaCount(
        NachaProfileRecordReader? profileReader,
        string record,
        EntryDetail entry,
        NachaAddendaBounds? publishedBounds = null,
        bool? publishedHasAddendaCount = null)
        => ResolveExpectedAddendaCountCore(profileReader, record, entry, publishedBounds, publishedHasAddendaCount);

    internal void ValidatePublishedBatchService(NachaInboundPublishedAuthority authority, BatchHeader batch)
    {
        var service = (batch.StandardEntryClassCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!authority.Services.Contains(service)
            || authority.Snapshot.Profile.ServiceClassCode is { } selectedService && service != selectedService
            || !authority.ServiceClasses.TryGetRule(batch.ServiceClassCode ?? string.Empty, out _))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T5_SERVICE_MISMATCH");
        }
    }

    internal void ValidatePublishedAddenda(
        NachaInboundPublishedAuthority authority,
        EntryDetail entry,
        AddendaRecord addenda,
        int parsedCount)
    {
        if (!string.Equals((addenda.CodeTypeAddendumRecord ?? string.Empty).Trim(), authority.T7AddendaType,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T7_TYPE_MISMATCH");
        }
        if (!authority.IsCenit)
        {
            return;
        }

        var expectedSequence = authority.T7SequenceConstant
            ?? parsedCount.ToString($"D{authority.T7SequenceLength}", CultureInfo.InvariantCulture);
        if (!string.Equals((addenda.AddendumSequence ?? string.Empty).Trim(), expectedSequence, StringComparison.Ordinal)
            || !string.Equals((addenda.EntryDetailSequenceNumber ?? string.Empty).Trim(),
                GetEntrySequenceSuffix(entry.SequenceNumber), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T7_SEQUENCE_OR_ASSOCIATION_MISMATCH");
        }
    }

    internal DateTime ValidateEntrySequenceStructure(
        EntryDetail entry,
        BatchHeader? batch,
        NachaHeader? header,
        Dictionary<int, int> lastConsecutiveByBatch,
        HashSet<string> seenSequenceNumbers,
        bool enforceBatchSequenceOrder)
    {
        if (batch is null)
        {
            ThrowTechnical("Error Fatal ID 7: no se encontró lote (registro tipo 5) para validar el Número de Secuencia del registro tipo 6.");
        }

        var rawSequence = (entry.SequenceNumber ?? string.Empty).Trim();
        if (rawSequence.Length != 15 || rawSequence.Any(c => !char.IsDigit(c)))
        {
            ThrowTechnical("Error Fatal ID 7: el Número de Secuencia del registro tipo 6 (posiciones 88-102) debe contener exactamente 15 dígitos numéricos.");
        }

        if (!seenSequenceNumbers.Add(rawSequence))
        {
            ThrowRegulatory("D04", "Número de secuencia duplicado en el archivo.");
        }

        var originSegment = rawSequence[..8];
        var consecutiveSegment = rawSequence[8..];

        var batchOrigin = (batch.OriginParticipantEntityCode ?? string.Empty).Trim();
        if (batchOrigin.Length != 8 || batchOrigin.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el código de entidad originadora del registro tipo 5 debe ser numérico de 8 dígitos para validar secuencia.");
        }

        if (!string.Equals(originSegment, batchOrigin, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento de entidad del Número de Secuencia no coincide con la entidad originadora del lote.");
        }

        var consecutiveValue = int.Parse(consecutiveSegment, CultureInfo.InvariantCulture);
        if (consecutiveValue < 1)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia debe iniciar en 0000001 o superior.");
        }

        if (consecutiveValue > 6_999_999)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia excede 6999999. El rango 7000001-9999999 está reservado para PSE.");
        }

        if (enforceBatchSequenceOrder
            && lastConsecutiveByBatch.TryGetValue(batch.BatchNumber, out var previousConsecutive)
            && consecutiveValue <= previousConsecutive)
        {
            ThrowRegulatory("D04", "La secuencia del lote no es estrictamente ascendente.");
        }

        if (enforceBatchSequenceOrder)
        {
            lastConsecutiveByBatch[batch.BatchNumber] = consecutiveValue;
        }

        return ParseNachaProcessingDate(header?.FileCreationDate)
            ?? throw new InvalidOperationException(
                "ACHCOL-T1-FILE-CREATION-DATE: no existe una fecha operacional válida para comprobar duplicidad de secuencia.");
    }

    internal void ValidateBatchSequenceAndControlConsistency(
        IEnumerable<BatchHeader> batchHeaders,
        IEnumerable<BatchControl> batchControls)
    {
        var headers = batchHeaders.ToList();
        var controls = batchControls.ToList();

        if (headers.Count == 0)
        {
            return;
        }

        if (headers.Count != controls.Count)
        {
            ThrowRegulatory("D04", "La cantidad de registros tipo 5 no coincide con los registros tipo 8 del archivo.");
        }

        for (int index = 0; index < headers.Count; index++)
        {
            var expectedBatchNumber = index + 1;
            var header = headers[index];
            var control = controls[index];

            if (header.BatchNumber != expectedBatchNumber)
            {
                throw new InvalidOperationException("Error Fatal ID 5: el Número de Lote del registro tipo 5 debe iniciar en 0000001 y ser secuencial ascendente.");
            }

            if (!int.TryParse(control.BatchNumber?.Trim(), out var controlBatchNumber))
            {
                ThrowRegulatory("D04", "El Número de Lote del registro tipo 8 debe ser numérico en posiciones 100-106.");
            }

            if (controlBatchNumber != expectedBatchNumber || controlBatchNumber != header.BatchNumber)
            {
                ThrowRegulatory("D04", "Inconsistencia en Número de Lote entre registros tipo 5 y tipo 8.");
            }
        }
    }

    internal void ValidateCurrentBatchControl(
        BatchHeader? header,
        NachaInboundBatchRuntimeMetrics? metrics,
        BatchControl control)
    {
        if (header is null || metrics is null)
        {
            ThrowRegulatory("D06", "Se recibió un registro tipo 8 sin un registro tipo 5 asociado.");
        }

        var serviceClassCode = (control.BatchTranClassCode ?? string.Empty).Trim();
        if (!string.Equals(serviceClassCode, (header.ServiceClassCode ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: el Código Clase de Transacciones del registro tipo 8 no coincide con el del registro tipo 5.");
        }

        if ((control.EntryAddendaCount ?? 0) != metrics.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"El conteo físico de registros tipo 6 y 7 del lote ({metrics.EntryAddendaCount}) no coincide con el valor reportado en el registro tipo 8 ({control.EntryAddendaCount ?? 0}).");
        }

        if ((control.EntryHash ?? 0) != metrics.EntryHash)
        {
            ThrowRegulatory("D05", "El Total de Control calculado del lote no coincide con el registro tipo 8.");
        }

        if (control.TotalDebitAmount != metrics.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 8: el total de débitos calculado del lote no coincide con el registro tipo 8.");
        }

        if (control.TotalCreditAmount != metrics.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 8: el total de créditos calculado del lote no coincide con el registro tipo 8.");
        }

        if (!string.Equals((control.IdUserOrig ?? string.Empty).Trim(), (header.CompanyId ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: la Identificación del Usuario Originador del registro tipo 8 no coincide con el registro tipo 5.");
        }

        if (!string.IsNullOrEmpty(control.Reserved) && control.Reserved.Any(c => c != ' '))
        {
            throw new InvalidOperationException("Error Fatal 87: el campo reservado del registro tipo 8 (posiciones 86-91) debe contener únicamente espacios en blanco.");
        }

        if (!string.Equals((control.IdOrigEntity ?? string.Empty).Trim(), (header.OriginParticipantEntityCode ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: la Identificación de la Entidad Participante Originadora del registro tipo 8 no coincide con el registro tipo 5.");
        }
    }

    internal void ValidateFileControlConsistency(
        IEnumerable<BatchHeader> batchHeaders,
        IEnumerable<BatchControl> batchControls,
        IEnumerable<FileControl> fileControls,
        NachaInboundFileRuntimeMetrics fileMetrics,
        IReadOnlyList<string> lines,
        int fileControlLineIndex)
    {
        var headers = batchHeaders.ToList();
        if (headers.Count == 0)
        {
            return;
        }

        var controls = fileControls.ToList();
        if (controls.Count != 1)
        {
            ThrowRegulatory("D06", "El archivo debe contener exactamente un Registro Tipo 9 de control de archivo.");
        }

        if (fileControlLineIndex < 0)
        {
            ThrowRegulatory("D06", "No se pudo ubicar el Registro Tipo 9 dentro del archivo.");
        }

        var control = controls[0];
        var batchControlList = batchControls.ToList();
        var expectedBatchCount = headers.Count;
        if (fileMetrics.BatchCount != expectedBatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de lotes calculada durante el recorrido del archivo ({fileMetrics.BatchCount}) no coincide con los registros tipo 5 ({expectedBatchCount}).");
        }

        if (control.BatchCount != expectedBatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de lotes del Registro Tipo 9 ({control.BatchCount}) no coincide con la cantidad de registros tipo 5 ({expectedBatchCount}).");
        }

        if (lines.Count % 10 != 0)
        {
            ThrowRegulatory("D02", $"El archivo debe ocupar un número entero de bloques de 10 registros y se recibieron {lines.Count} registros.");
        }

        var expectedBlockCount = lines.Count / 10;
        if (control.BlockCount != expectedBlockCount)
        {
            ThrowRegulatory("D04", $"El número de bloques físicos del Registro Tipo 9 ({control.BlockCount}) no coincide con los bloques del archivo ({expectedBlockCount}).");
        }

        if (control.EntryAddendaCount != fileMetrics.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"El conteo total de registros tipo 6 y 7 del archivo ({fileMetrics.EntryAddendaCount}) no coincide con el Registro Tipo 9 ({control.EntryAddendaCount}).");
        }

        if (control.EntryHash != fileMetrics.EntryHash)
        {
            ThrowRegulatory("D05", "El Hash Total calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (control.TotalDebitAmount != fileMetrics.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 62: el total de débitos calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (control.TotalCreditAmount != fileMetrics.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 63: el total de créditos calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (!string.IsNullOrEmpty(control.Reserved) && control.Reserved.Any(c => c != ' '))
        {
            throw new InvalidOperationException("Error Fatal 9: el campo reservado del Registro Tipo 9 (posiciones 68-106) debe contener únicamente espacios en blanco.");
        }

        for (int index = fileControlLineIndex + 1; index < lines.Count; index++)
        {
            if (!IsPaddingRecord(lines[index]))
            {
                ThrowRegulatory("D02", "Los registros de relleno posteriores al Registro Tipo 9 deben contener únicamente el carácter '9' en sus 106 posiciones.");
            }
        }

        var aggregatedBatchCount = batchControlList.Count;
        if (aggregatedBatchCount != control.BatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de registros tipo 8 ({aggregatedBatchCount}) no coincide con la cantidad de lotes reportada en el Registro Tipo 9 ({control.BatchCount}).");
        }

        var aggregatedEntryAddendaCount = batchControlList.Sum(batchControl => batchControl.EntryAddendaCount ?? 0);
        if (aggregatedEntryAddendaCount != control.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"La sumatoria de conteos de los registros tipo 8 ({aggregatedEntryAddendaCount}) no coincide con el Registro Tipo 9 ({control.EntryAddendaCount}).");
        }

        const long maxHash = 10_000_000_000L;
        var aggregatedHash = batchControlList.Aggregate(0L, (current, batchControl) => (current + (batchControl.EntryHash ?? 0)) % maxHash);
        if (aggregatedHash != control.EntryHash)
        {
            ThrowRegulatory("D05", "La sumatoria de hashes de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }

        var aggregatedDebit = batchControlList.Sum(batchControl => batchControl.TotalDebitAmount);
        if (aggregatedDebit != control.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 62: la sumatoria de débitos de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }

        var aggregatedCredit = batchControlList.Sum(batchControl => batchControl.TotalCreditAmount);
        if (aggregatedCredit != control.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 63: la sumatoria de créditos de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }
    }

    internal List<EntryDetail> EnforceAddendaRequirements(
        List<EntryDetail> entries,
        List<AddendaRecord> addendaRecords,
        List<NachaValidationFailure> failures,
        HashSet<string> optionalAddendaEntries)
    {
        var validEntries = new List<EntryDetail>();
        foreach (var entry in entries)
        {
            var relatedAddendas = addendaRecords
                .Where(addenda => IsAddendaForEntry(entry, addenda))
                .ToList();

            if (!relatedAddendas.Any()
                && !optionalAddendaEntries.Contains(entry.SequenceNumber ?? string.Empty))
            {
                failures.Add(new NachaValidationFailure("6", null, entry.SequenceNumber, entry.TransactionCode,
                    "No se encontró registro 7 asociado al detalle."));
                continue;
            }

            if (NachaInboundTransactionCodeSets.ReturnCodes.Contains(entry.TransactionCode ?? string.Empty))
            {
                var hasReturnAddenda = relatedAddendas.Any(addenda =>
                    string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase));

                if (!hasReturnAddenda)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "Las devoluciones (21/26/31/36/51/56) deben incluir adenda tipo 99."));
                    continue;
                }

                var hasReturnReason = relatedAddendas.Any(HasReturnReason);
                if (!hasReturnReason)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La adenda de devolución debe incluir causal Rxx."));
                    continue;
                }

                var hasOriginalTraceReference = relatedAddendas.Any(addenda =>
                    !string.IsNullOrWhiteSpace(addenda.OriginalTraceNumber) &&
                    addenda.OriginalTraceNumber.Trim().Length == 15);

                if (!hasOriginalTraceReference)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La devolución debe incluir referencia de secuencia original en la adenda."));
                    continue;
                }
            }

            validEntries.Add(entry);
        }

        return validEntries;
    }

    internal static bool IsPaddingRecord(string line)
        => line.Length == 106 && line.All(character => character == '9');

    internal static bool IsAddendaForEntry(EntryDetail entry, AddendaRecord addenda)
    {
        var entrySequence = GetEntrySequenceSuffix(entry.SequenceNumber);
        return !string.IsNullOrWhiteSpace(entrySequence) &&
               string.Equals(addenda.EntryDetailSequenceNumber, entrySequence, StringComparison.OrdinalIgnoreCase);
    }

    internal static string? GetEntrySequenceSuffix(string? sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return null;
        }

        return sequence.Length <= 7 ? sequence : sequence[^7..];
    }

    internal void EnsureNoPendingAddenda(bool pending, string message)
    {
        if (pending)
        {
            ThrowTechnical(message);
        }
    }

    internal void EnsureAddendaCanAttach(EntryDetail? lastEntry, bool awaitingAddenda)
    {
        if (lastEntry is null || !awaitingAddenda)
        {
            ThrowTechnical("ACHCOL-T6-T7-ORDER: se recibió T7 sin un T6 inmediatamente asociado que declare adenda.");
        }
    }

    [DoesNotReturn]
    internal void ThrowRegulatory(string dxxCode, string fallbackMessage)
        => ThrowRegulatoryCore(dxxCode, fallbackMessage);

    [DoesNotReturn]
    internal static void ThrowTechnical(string message)
        => throw new InvalidOperationException($"[{ValidationBoundary.Technical}] {message}");

    private static NachaInboundPublishedAuthority ResolvePublishedInboundAuthority(
        NachaPublicationSnapshot snapshot,
        NachaParseRequest request)
    {
        var identity = snapshot.Profile;
        if (identity.ProfileId != request.SelectedProfileId
            || !string.Equals(identity.ProfileCode, request.SelectedProfileCode, StringComparison.Ordinal)
            || identity.DirectionCode != "ENTRADA"
            || identity.ClearingHouseCode is not ("ACH" or "CENIT")
            || identity.FlowTypeCode is not ("ORIGINAL" or "PRENOTIFICACION"))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_IDENTITY_MISMATCH");
        }

        foreach (var recordCode in new[] { "1", "5", "6", "7", "8", "9" })
        {
            if (snapshot.Records.Count(record => record.IsEnabled && record.RecordCode == recordCode) != 1
                || !snapshot.LayoutVariants.Any(variant => variant.RecordCode == recordCode))
            {
                throw new InvalidOperationException("INBOUND_PUBLISH_RECORD_MISSING");
            }
        }

        var transactionCodes = NachaTransactionCodeSemanticMetadata.Resolve(snapshot.Records);
        if (transactionCodes.Status != NachaTransactionCodeSemanticMetadataStatus.Resolved
            || transactionCodes.Contract is null)
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T6_SEMANTICS_UNRESOLVED");
        }

        var t5 = snapshot.Records.Single(record => record.IsEnabled && record.RecordCode == "5");
        var declarations = t5.SemanticRuleSet?.ResolvedDeclarations;
        if (declarations is null
            || declarations.Count == 0
            || declarations.Any(rule => string.IsNullOrWhiteSpace(rule.ServiceClassCode))
            || declarations.Select(rule => rule.ServiceClassCode).Distinct(StringComparer.Ordinal).Count() != declarations.Count
            || !NachaConfigResolver.TryGetPublishedSecValues(snapshot, out var services))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T5_SEMANTICS_UNRESOLVED");
        }

        if (identity.ServiceClassCode is { } selectedService && !services.Contains(selectedService))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_SERVICE_MISMATCH");
        }

        var t6Fields = snapshot.LayoutVariants
            .Where(variant => variant.RecordCode == "6" && variant.IsDefaultForRecord)
            .SelectMany(variant => variant.Fields.Where(field => field.IsEnabled))
            .ToArray();
        var indicatorField = t6Fields.SingleOrDefault(field => field.FieldCode == "ADDENDARECORDINDICATOR");
        var indicatorRule = indicatorField?.Rules.SingleOrDefault(rule => rule.IsEnabled && rule.RuleTypeCode == "ENUM");
        if (indicatorRule?.RuleConfiguration is not { } indicatorConfiguration
            || indicatorConfiguration.ValueKind != JsonValueKind.Object
            || !indicatorConfiguration.TryGetProperty("allowedValues", out var indicatorValues)
            || indicatorValues.ValueKind != JsonValueKind.Array
            || indicatorValues.GetArrayLength() == 0
            || indicatorValues.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString())))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T6_ADDENDA_INDICATOR_UNRESOLVED");
        }
        var addendaIndicators = indicatorValues.EnumerateArray()
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        if (addendaIndicators.Count != indicatorValues.GetArrayLength())
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T6_ADDENDA_INDICATOR_DUPLICATE");
        }
        var hasT6AddendaCount = t6Fields.Count(field => field.FieldCode == "ADDENDACOUNT") == 1;
        if (identity.ClearingHouseCode == "CENIT"
            && hasT6AddendaCount != (identity.ServiceClassCode == "CTX"))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_CTX_LAYOUT_MISMATCH");
        }

        NachaAddendaCardinalityPolicy? cardinality = null;
        if (identity.ClearingHouseCode == "CENIT")
        {
            var resolved = NachaAddendaCardinalityMetadata.Resolve(snapshot.GenerationCriticalTags
                .Select(tag => new KeyValuePair<string, string>(tag.Key, tag.Value)));
            if (resolved.Status != NachaAddendaCardinalityMetadataStatus.Resolved
                || resolved.Policy is null
                || resolved.Policy.DirectionCode != identity.DirectionCode
                || resolved.Policy.FlowTypeCode != identity.FlowTypeCode
                || services.Any(service =>
                    !resolved.Policy.TryResolve(service, NachaEntryDirection.Credit, out _)
                    || !resolved.Policy.TryResolve(service, NachaEntryDirection.Debit, out _)))
            {
                throw new InvalidOperationException("CARDINALITY_POLICY_UNRESOLVED");
            }
            cardinality = resolved.Policy;
        }

        var t7Variants = snapshot.LayoutVariants.Where(variant => variant.RecordCode == "7").ToArray();
        string? addendaType = null;
        string? sequenceConstant = null;
        var sequenceLength = 0;
        foreach (var variant in t7Variants)
        {
            var typeField = variant.Fields.SingleOrDefault(field => field.IsEnabled && field.FieldCode == "ADDENDATYPE");
            var typeRule = typeField?.Rules.SingleOrDefault(rule => rule.IsEnabled && rule.RuleTypeCode == "ENUM");
            if (typeRule?.RuleConfiguration is not { } configuration
                || configuration.ValueKind != JsonValueKind.Object
                || !configuration.TryGetProperty("allowedValues", out var values)
                || values.ValueKind != JsonValueKind.Array
                || values.GetArrayLength() != 1
                || values[0].ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(values[0].GetString()))
            {
                throw new InvalidOperationException("INBOUND_PUBLISH_T7_TYPE_UNRESOLVED");
            }
            var value = values[0].GetString()!;
            if (addendaType is not null && addendaType != value)
            {
                throw new InvalidOperationException("INBOUND_PUBLISH_T7_TYPE_CONFLICT");
            }
            addendaType = value;

            if (identity.ClearingHouseCode != "CENIT")
            {
                continue;
            }
            var sequenceField = variant.Fields.SingleOrDefault(field => field.IsEnabled && field.FieldCode == "SEQUENCENUMBER")
                ?? throw new InvalidOperationException("INBOUND_PUBLISH_T7_SEQUENCE_MISSING");
            if (sequenceLength != 0 && sequenceLength != sequenceField.Length)
            {
                throw new InvalidOperationException("INBOUND_PUBLISH_T7_SEQUENCE_CONFLICT");
            }
            sequenceLength = sequenceField.Length;
            sequenceConstant = sequenceField.Source.DataSourceTypeCode == "CONSTANTE"
                ? sequenceField.Source.ConstantValue
                : null;
        }

        if (addendaType is null
            || identity.ClearingHouseCode == "CENIT" && (sequenceLength <= 0
                || hasT6AddendaCount == (sequenceConstant is not null)))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T7_SEMANTICS_UNRESOLVED");
        }

        return new NachaInboundPublishedAuthority(snapshot, transactionCodes.Contract,
            new NachaServiceClassSemanticContract(declarations.Select(rule =>
                new NachaServiceClassSemanticRule(rule.ServiceClassCode, rule.AllowsCredit, rule.AllowsDebit))),
            services, addendaIndicators, cardinality, hasT6AddendaCount, addendaType, sequenceConstant, sequenceLength);
    }

    private static int ResolveExpectedAddendaCountCore(
        NachaProfileRecordReader? profileReader,
        string record,
        EntryDetail entry,
        NachaAddendaBounds? publishedBounds = null,
        bool? publishedHasAddendaCount = null)
    {
        if (!(publishedHasAddendaCount
              ?? CenitOrdinaryInbound2026Layout.IsCtxProfile(profileReader?.ProfileCode)))
        {
            var declared = string.Equals(entry.AddendumIndicator, "1", StringComparison.Ordinal) ? 1 : 0;
            ValidatePublishedAddendaCount(declared, publishedBounds);
            return declared;
        }

        if (!profileReader!.TryRead(record, "6", "ADDENDACOUNT", null, out var rawCount)
            || !int.TryParse(rawCount, NumberStyles.None, CultureInfo.InvariantCulture, out var count))
        {
            throw new InvalidOperationException("CENIT-2026-CTX-IN-T6-ADDENDA-COUNT: el número de adendas debe ser numérico de cuatro posiciones.");
        }

        if (publishedBounds is not null)
        {
            ValidatePublishedAddendaCount(count, publishedBounds);
        }
        else if (publishedHasAddendaCount is null)
        {
            NachaInboundCenitValidationRules.ValidateCtxAddendaCardinality(count);
            if (CenitOrdinaryInbound2026Layout.IsPrenotificationProfile(profileReader.ProfileCode) && count != 1)
            {
                throw new InvalidOperationException("CENIT-2026-CTX-IN-T6-ADDENDA-COUNT: la prenotificación CTX requiere exactamente una adenda.");
            }
        }
        if (!string.Equals(entry.AddendumIndicator, "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CENIT-2026-CTX-IN-T6-ADDENDA-INDICATOR: el indicador debe ser 1 cuando la transacción declara adendas.");
        }

        return count;
    }

    private static void ValidatePublishedAddendaCount(int count, NachaAddendaBounds? bounds)
    {
        if (bounds is not null && (count < bounds.Minimum || count > bounds.Maximum))
        {
            throw new InvalidOperationException("CENIT_ADDENDA_CARDINALITY_INVALID: la cantidad declarada de adendas no cumple la política publicada.");
        }
    }

    private static DateTime? ParseNachaProcessingDate(string? fileCreationDate)
    {
        if (string.IsNullOrWhiteSpace(fileCreationDate))
        {
            return null;
        }

        var value = fileCreationDate.Trim();
        if (DateTime.TryParseExact(value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyMMdd))
        {
            return yyMMdd.Date;
        }

        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyyyMMdd))
        {
            return yyyyMMdd.Date;
        }

        return null;
    }

    private static bool HasReturnReason(AddendaRecord addenda)
        => !string.IsNullOrWhiteSpace(addenda.ReturnReasonCode)
           && Regex.IsMatch(addenda.ReturnReasonCode.Trim(), @"^R\d{2}$", RegexOptions.IgnoreCase);

    private string GetRegulatoryError(string dxxCode, string fallbackMessage)
    {
        if (_rejectionCatalog.TryGetValue(dxxCode, out var catalog))
        {
            return $"{catalog.Code}: {catalog.Description}. {fallbackMessage}";
        }

        return $"{dxxCode}: {fallbackMessage}";
    }

    [DoesNotReturn]
    private void ThrowRegulatoryCore(string dxxCode, string fallbackMessage)
        => throw new InvalidOperationException($"[{ValidationBoundary.Regulatory}] {GetRegulatoryError(dxxCode, fallbackMessage)}");
}

/// <summary>
/// CENIT-only inbound validation rules. Kept separate from generic/PUBLISH validation so CENIT
/// record semantics cannot bleed into ACH Colombia compatibility behavior.
/// </summary>
internal static class NachaInboundCenitValidationRules
{
    internal static void ValidateBatchService(string profileCode, BatchHeader batch)
    {
        var service = (batch.StandardEntryClassCode ?? string.Empty).Trim().ToUpperInvariant();
        var valid = CenitOrdinaryInbound2026Layout.IsCtxProfile(profileCode)
            ? service == "CTX"
            : service is "PPD" or "CCD";
        if (!valid)
        {
            throw new InvalidOperationException("CENIT_INBOUND_PROFILE_SERVICE_MISMATCH: el servicio del lote no corresponde al perfil oficial seleccionado.");
        }

        var serviceClass = (batch.ServiceClassCode ?? string.Empty).Trim();
        if (serviceClass is not ("200" or "220" or "225"))
        {
            throw new InvalidOperationException("CENIT-2026-T5-SERVICE-CLASS: el código clase de transacciones no es válido.");
        }
    }

    internal static void ValidateEntry(string profileCode, EntryDetail entry)
    {
        var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
        var descriptor = CenitOrdinaryInbound2026Layout.Field(profileCode, "6", "TRANSACTIONCODE");
        if (descriptor.AllowedValues is null
            || !descriptor.AllowedValues.Any(value => string.Equals(value, transactionCode, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("CENIT-2026-T6-TRANSACTION-CODE: el código de transacción no pertenece al perfil ordinario CENIT.");
        }

        var isPrenotification = NachaInboundTransactionCodeSets.PrenoteCodes.Contains(transactionCode);
        if (isPrenotification != CenitOrdinaryInbound2026Layout.IsPrenotificationProfile(profileCode))
        {
            throw new InvalidOperationException("CENIT_INBOUND_PROFILE_FLOW_MISMATCH: la familia monetaria/prenotificación no corresponde al perfil seleccionado.");
        }
    }

    internal static void ValidateAddenda(
        EntryDetail entry,
        AddendaRecord addenda,
        int parsedCount,
        bool isCtx)
    {
        if (!string.Equals((addenda.CodeTypeAddendumRecord ?? string.Empty).Trim(), "05", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CENIT-2026-T7-ADDENDA-TYPE: el tipo de adenda de información debe ser 05.");
        }

        var expectedSequence = isCtx ? parsedCount.ToString("0000", CultureInfo.InvariantCulture) : "0001";
        if (!string.Equals((addenda.AddendumSequence ?? string.Empty).Trim(), expectedSequence, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CENIT-2026-CTX-IN-T7-SEQUENCE: la secuencia de adendas debe ser consecutiva y reiniciar por cada transacción.");
        }

        var expectedAssociation = NachaInboundValidator.GetEntrySequenceSuffix(entry.SequenceNumber);
        if (!string.Equals((addenda.EntryDetailSequenceNumber ?? string.Empty).Trim(), expectedAssociation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CENIT-2026-CTX-IN-T7-ASSOCIATION: la adenda no referencia la secuencia de su registro de detalle propietario.");
        }
    }

    internal static void ValidateCtxAddendaCardinality(int count)
    {
        if (count is < 1 or > CenitCtxOutbound2026Layout.MaxAddendaPerEntry)
        {
            throw new InvalidOperationException("CENIT-2026-CTX-IN-T6-ADDENDA-COUNT: cada transacción CTX debe contener entre 1 y 9999 adendas.");
        }
    }
}

/// <summary>
/// ACH Colombia compatibility rules that are not part of the canonical PUBLISH snapshot contract.
/// </summary>
internal static class NachaInboundAchColombiaValidationRules
{
    internal static bool IsPseCcdBatch(BatchHeader? batch)
    {
        if (batch is null)
        {
            return false;
        }

        if (!string.Equals(batch.StandardEntryClassCode?.Trim(), "CCD", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pseHints = new[]
        {
            batch.CompanyEntryDescription,
            batch.CompanyName,
            batch.DiscretionaryData,
            batch.CompanyId
        };

        return pseHints.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains("PSE", StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class NachaInboundDatabaseValidator
{
    private readonly AchDbContext _context;
    private readonly NachaInboundValidator _rules;
    private readonly bool _localLiveProcTransaccionesPreparationEnabled;
    private HashSet<string>? _configuredTransactionCodes;

    internal NachaInboundDatabaseValidator(
        AchDbContext context,
        NachaInboundValidator rules,
        bool localLiveProcTransaccionesPreparationEnabled)
    {
        _context = context;
        _rules = rules;
        _localLiveProcTransaccionesPreparationEnabled = localLiveProcTransaccionesPreparationEnabled;
    }

    internal async Task ValidateEntrySequencePolicyAsync(
        EntryDetail entry,
        BatchHeader? batch,
        NachaHeader? header,
        Dictionary<int, int> lastConsecutiveByBatch,
        HashSet<string> seenSequenceNumbers,
        bool enforceBatchSequenceOrder,
        CancellationToken ct)
    {
        var processingDate = _rules.ValidateEntrySequenceStructure(
            entry,
            batch,
            header,
            lastConsecutiveByBatch,
            seenSequenceNumbers,
            enforceBatchSequenceOrder);
        var rawSequence = (entry.SequenceNumber ?? string.Empty).Trim();
        var existsInPreviousCycle = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.EffectiveEntryDate.Date == processingDate
                           && t.TraceNumber == rawSequence, ct);

        if (existsInPreviousCycle)
        {
            _rules.ThrowRegulatory("D01", "Número de secuencia duplicado en el ámbito de fecha operacional.");
        }
    }

    internal async Task<(bool IsValid, string? FailureReason)> ValidateEntryAsync(
        EntryDetail entry,
        BatchHeader? batch,
        bool isReturnEntry,
        List<NachaValidationFailure> failures,
        bool optionalAddenda,
        NachaTransactionCodeSemanticRule? publishedRule,
        NachaInboundPublishedAuthority? publishedAuthority,
        CancellationToken ct)
    {
        var code = entry.TransactionCode ?? string.Empty;
        if (publishedRule is null && !((await GetConfiguredTransactionCodesAsync(ct)).Contains(code)))
        {
            const string reason = "Código de transacción inválido.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var isCredit = publishedRule?.Direction == NachaEntryDirection.Credit
                       || publishedRule is null && NachaInboundTransactionCodeSets.CreditCodes.Contains(code);
        var isDebit = publishedRule?.Direction == NachaEntryDirection.Debit
                      || publishedRule is null && NachaInboundTransactionCodeSets.DebitCodes.Contains(code);
        var serviceClassCode = batch?.ServiceClassCode?.Trim();
        var isPseCcdCredit = isCredit && NachaInboundAchColombiaValidationRules.IsPseCcdBatch(batch);

        if (publishedAuthority is not null
            && (!publishedAuthority.ServiceClasses.TryGetRule(serviceClassCode ?? string.Empty, out var serviceRule)
                || !serviceRule.Allows(publishedRule!.Direction)))
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T5_T6_DIRECTION_MISMATCH");
        }

        if (publishedAuthority is null && serviceClassCode == "220" && !isCredit)
        {
            const string reason = "Lote exclusivo de crédito (220) no permite débitos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (publishedAuthority is null && serviceClassCode == "225" && !isDebit)
        {
            const string reason = "Lote exclusivo de débito (225) no permite créditos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if ((publishedRule?.IsPrenotification ?? NachaInboundTransactionCodeSets.PrenoteCodes.Contains(code))
            && entry.Amount.GetValueOrDefault() != 0m)
        {
            const string reason = "Prenotificación debe tener valor 0.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (publishedAuthority is not null
            ? !publishedAuthority.AddendaIndicators.Contains(entry.AddendumIndicator ?? string.Empty)
            : !string.Equals(entry.AddendumIndicator, "1", StringComparison.OrdinalIgnoreCase)
              && !(optionalAddenda && string.Equals(entry.AddendumIndicator, "0", StringComparison.Ordinal)))
        {
            const string reason = "El registro 7 es obligatorio para todas las transacciones.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (publishedAuthority is not null && isReturnEntry)
        {
            throw new InvalidOperationException("INBOUND_PUBLISH_T7_RETURN_IN_ORDINARY");
        }
        if (publishedAuthority is null && isReturnEntry && !NachaInboundTransactionCodeSets.ReturnCodes.Contains(code))
        {
            const string reason = "La adenda de devolución no corresponde a un código de transacción de devolución.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var checkDigitValidationReason = await ValidateType6CheckDigitAsync(entry, ct);
        if (checkDigitValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, checkDigitValidationReason));
            return (false, checkDigitValidationReason);
        }

        var receiverNameValidationReason = NachaReceiverNameHelper.ValidateType6RawField(entry.RecipUserName);
        if (receiverNameValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, receiverNameValidationReason));
            return (false, receiverNameValidationReason);
        }

        // An inbound Return keeps the original-operation data. Functional correlation remains
        // in post-processing and is deliberately not moved into R5B.
        if (isReturnEntry)
        {
            return (true, null);
        }

        var requiresIdentityValidation = isDebit || ShouldValidateCreditIdentity(entry.DiscreData);

        if (isPseCcdCredit)
        {
            var accountNumber = entry.AccountNumber ?? string.Empty;
            var accountExists = await _context.CustomerAccounts
                .AsNoTracking()
                .AnyAsync(a => a.AccountNumber == accountNumber, ct);

            if (!accountExists)
            {
                const string reason = "R03: Cuenta de destino no existe en la entidad receptora.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            return (true, null);
        }

        if (requiresIdentityValidation)
        {
            if (string.IsNullOrWhiteSpace(entry.RecipIdNumber))
            {
                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            var accountNumber = entry.AccountNumber ?? string.Empty;
            var recipientId = entry.RecipIdNumber ?? string.Empty;
            var matches = await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == accountNumber), ct);

            if (!matches)
            {
                if (_localLiveProcTransaccionesPreparationEnabled
                    && string.Equals(code, "32", StringComparison.Ordinal))
                {
                    return (true, "LOCAL_LIVE_RECIPIENT_PREPARATION_PENDING");
                }

                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }
        }

        return (true, null);
    }

    private async Task<string?> ValidateType6CheckDigitAsync(EntryDetail entry, CancellationToken ct)
    {
        var receivingCode = (entry.ReceivingParticipantEntityCode ?? string.Empty).Trim();
        if (receivingCode.Length != 8 || receivingCode.Any(c => !char.IsDigit(c)))
        {
            return "Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener 8 dígitos numéricos.";
        }

        var fileCheckDigit = (entry.CheckDigit ?? string.Empty).Trim();
        if (fileCheckDigit.Length != 1 || !char.IsDigit(fileCheckDigit[0]))
        {
            return "Error Fatal ID 35: el Dígito de Chequeo (posición 12) debe ser numérico de longitud 1.";
        }

        var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingCode);
        if (!string.Equals(fileCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
        {
            return "Error Fatal ID 35: el Dígito de Chequeo (posición 12) no corresponde al Código Entidad Participante Receptor (posiciones 4-11).";
        }

        var institution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => (fi.RoutingNumber + fi.TransitCode) == receivingCode, ct);

        if (institution is not null)
        {
            var dbCheckDigit = (institution.CheckDigit ?? string.Empty).Trim();
            if (dbCheckDigit.Length != 1 || !char.IsDigit(dbCheckDigit[0]))
            {
                return "Error Fatal ID 35: el dígito de chequeo almacenado en FinancialInstitutions no es válido.";
            }

            if (!string.Equals(dbCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                return "Error Fatal ID 35: inconsistencia en FinancialInstitutions.CheckDigit.";
            }
        }

        return null;
    }

    private async Task<HashSet<string>> GetConfiguredTransactionCodesAsync(CancellationToken ct)
    {
        if (_configuredTransactionCodes is not null)
        {
            return _configuredTransactionCodes;
        }

        var configuredCodes = await _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(ct);

        _configuredTransactionCodes = configuredCodes.Count == 0
            ? new HashSet<string>(NachaInboundTransactionCodeSets.FallbackTransactionCodes, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(configuredCodes, StringComparer.OrdinalIgnoreCase);

        return _configuredTransactionCodes;
    }

    private static bool ShouldValidateCreditIdentity(string? discretionaryData)
        => !string.IsNullOrWhiteSpace(discretionaryData)
           && discretionaryData.StartsWith("V", StringComparison.OrdinalIgnoreCase);
}

internal sealed record NachaInboundValidationContext(
    NachaInboundPublishedAuthority? PublishedAuthority,
    bool IsCenitInboundProfile,
    bool IsCenitCtxInboundProfile);

internal sealed record NachaEntryAddendaPreparation(bool OptionalAddenda, NachaAddendaBounds? PublishedBounds);

internal sealed record NachaInboundPublishedAuthority(
    NachaPublicationSnapshot Snapshot,
    NachaTransactionCodeSemanticContract TransactionCodes,
    NachaServiceClassSemanticContract ServiceClasses,
    IReadOnlySet<string> Services,
    IReadOnlySet<string> AddendaIndicators,
    NachaAddendaCardinalityPolicy? Cardinality,
    bool HasT6AddendaCount,
    string T7AddendaType,
    string? T7SequenceConstant,
    int T7SequenceLength)
{
    internal bool IsCenit => Snapshot.Profile.ClearingHouseCode == "CENIT";
}

internal sealed class NachaInboundBatchRuntimeMetrics
{
    private const long MaxHash = 10_000_000_000L;

    internal int EntryAddendaCount { get; private set; }
    internal long EntryHash { get; private set; }
    internal decimal TotalDebitAmount { get; private set; }
    internal decimal TotalCreditAmount { get; private set; }

    internal void RegisterEntry(EntryDetail entry, NachaTransactionCodeSemanticRule? publishedRule)
    {
        EntryAddendaCount++;

        var entityCode = new string((entry.ReceivingParticipantEntityCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (entityCode.Length > 0)
        {
            var hashDigits = entityCode.Length > 8 ? entityCode[..8] : entityCode;
            if (long.TryParse(hashDigits, out var hashValue))
            {
                EntryHash = (EntryHash + hashValue) % MaxHash;
            }
        }

        var amount = entry.Amount ?? 0m;
        var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
        if (publishedRule?.Direction == NachaEntryDirection.Debit
            || publishedRule is null && NachaInboundTransactionCodeSets.DebitCodes.Contains(transactionCode))
        {
            TotalDebitAmount += amount;
        }
        else if (publishedRule?.Direction == NachaEntryDirection.Credit
                 || publishedRule is null && NachaInboundTransactionCodeSets.CreditCodes.Contains(transactionCode))
        {
            TotalCreditAmount += amount;
        }
    }

    internal void RegisterAddenda()
    {
        EntryAddendaCount++;
    }
}

internal sealed class NachaInboundFileRuntimeMetrics
{
    private const long MaxHash = 10_000_000_000L;

    internal int BatchCount { get; private set; }
    internal int EntryAddendaCount { get; private set; }
    internal long EntryHash { get; private set; }
    internal decimal TotalDebitAmount { get; private set; }
    internal decimal TotalCreditAmount { get; private set; }

    internal void RegisterBatch()
    {
        BatchCount++;
    }

    internal void RegisterEntry(EntryDetail entry, NachaTransactionCodeSemanticRule? publishedRule)
    {
        EntryAddendaCount++;

        var entityCode = new string((entry.ReceivingParticipantEntityCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (entityCode.Length > 0)
        {
            var hashDigits = entityCode.Length > 8 ? entityCode[..8] : entityCode;
            if (long.TryParse(hashDigits, out var hashValue))
            {
                EntryHash = (EntryHash + hashValue) % MaxHash;
            }
        }

        var amount = entry.Amount ?? 0m;
        var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
        if (publishedRule?.Direction == NachaEntryDirection.Debit
            || publishedRule is null && NachaInboundTransactionCodeSets.DebitCodes.Contains(transactionCode))
        {
            TotalDebitAmount += amount;
        }
        else if (publishedRule?.Direction == NachaEntryDirection.Credit
                 || publishedRule is null && NachaInboundTransactionCodeSets.CreditCodes.Contains(transactionCode))
        {
            TotalCreditAmount += amount;
        }
    }

    internal void RegisterAddenda()
    {
        EntryAddendaCount++;
    }
}

internal static class NachaInboundTransactionCodeSets
{
    internal static readonly IReadOnlySet<string> FallbackTransactionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "26", "27", "28",
        "31", "32", "33", "36", "37", "38",
        "42", "51", "52", "53", "55", "56", "57"
    };

    internal static readonly IReadOnlySet<string> CreditCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "31", "32", "33", "42", "51", "52", "53"
    };

    internal static readonly IReadOnlySet<string> DebitCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "26", "27", "28", "36", "37", "38", "55", "56", "57"
    };

    internal static readonly IReadOnlySet<string> PrenoteCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "23", "33", "53", "28", "38", "57"
    };

    internal static readonly IReadOnlySet<string> ReturnCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "21", "26", "31", "36", "51", "56"
    };
}
