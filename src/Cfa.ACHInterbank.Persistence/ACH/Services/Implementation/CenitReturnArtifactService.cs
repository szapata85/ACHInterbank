using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Conflicto de generación I05: nunca se reintenta ni se crea archivo por omisión.</summary>
public sealed class CenitReturnArtifactConflictException(string message) : Exception(message);

/// <summary>
/// Genera UN artefacto local de revisión a partir de una aprobación I04 vigente.
/// No usa AchReturnsService, no altera AchReturnsGenerated ni la transacción bancaria,
/// no publica nombre externo, no crea exportaciones y no invoca transporte.
/// Generación exclusivamente manual, nunca por job ni por modo AUTOMATICO.
/// </summary>
public sealed class CenitReturnArtifactService(
    AchDbContext context, TimeProvider clock, INachaFileBuilder builder,
    IAchReturnTraceSequenceService sequences, IDataProtectionProvider protection)
    : ICenitReturnArtifactService
{
    public const string Generated = "GENERADA_SIN_ENVIO";
    private readonly IDataProtector _protector = protection.CreateProtector(
        "Cfa.ACHInterbank.CENIT.Returns.I05.LocalArtifact.v1");

    public async Task<CenitReturnArtifactResult> GenerateAsync(Guid preparationId, string actorId,
        CancellationToken ct = default)
    {
        if (preparationId == Guid.Empty || !IsValidActor(actorId))
            throw new ArgumentException("Se requiere identificador e identidad autenticada.");

        if (context.Database.IsRelational())
            return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);
                var result = await GenerateAttemptAsync(preparationId, actorId, ct);
                await tx.CommitAsync(ct);
                return result;
            });
        return await GenerateAttemptAsync(preparationId, actorId, ct);
    }

    private async Task<CenitReturnArtifactResult> GenerateAttemptAsync(
        Guid id, string actor, CancellationToken ct)
    {
        var p = await LoadPreparationAsync(id, ct);
        if (p.Status != CenitReturnReviewService.Approved ||
            p.GeneratedAtUtc.HasValue || p.ProtectedArtifactContent is not null)
            throw Conflict("Solamente una preparación aprobada y aún no generada puede procesarse.");
        if (string.IsNullOrWhiteSpace(p.ReviewedByUserId) ||
            p.ReviewedByUserId.Equals("system", StringComparison.OrdinalIgnoreCase) ||
            p.ReviewedAtUtc is null ||
            string.IsNullOrWhiteSpace(p.DecisionEvidenceReference) ||
            string.IsNullOrWhiteSpace(p.DecisionJustification))
            throw Conflict("No hay aprobación humana íntegra y documentada.");

        // No confiar en un estado APROBADA falsificado; la primera identidad se acredita con
        // INSERT de AuditLogs, y el decisor debe ser una persona diferente.
        var firstAudit = await context.AuditLogs.AsNoTracking()
            .Where(x => x.EntityName == nameof(CenitReturnPreparation)
                && x.EntityId == p.Id.ToString() && x.BeforeJson == null && x.AfterJson != null)
            .OrderBy(x => x.ChangedAt).FirstOrDefaultAsync(ct);
        if (firstAudit is null || !IsValidActor(firstAudit.ChangedBy) ||
            string.Equals(firstAudit.ChangedBy, p.ReviewedByUserId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(firstAudit.ChangedBy, actor, StringComparison.OrdinalIgnoreCase))
            throw Conflict("No está acreditada la separación entre preparador, revisor y generador.");

        // I04 vuelve a validar la última evidencia del núcleo, causal, modo y la
        // ventana de compensación inmediatamente siguiente bajo la misma transacción.
        try { await new CenitReturnReviewService(context, clock).ValidateApprovalAsync(p, ct); }
        catch (CenitReturnReviewConflictException ex) { throw Conflict(ex.Message); }

        var original = p.OriginalTransaction;
        var houseId = original.AchCycle.ClearingHouseId;
        var source = await LoadUniqueRawSourceAsync(p, houseId, ct);
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var dfi = RequiredDigits(original.ReceivingDFI, 8, "DFI receptor original");
        var originalDfi = RequiredDigits(original.OriginatingDFI, 8, "DFI originador original");
        var originalTrace = RequiredDigits(source.Entry.SequenceNumber, 15, "secuencia original");
        if (!string.Equals(original.TraceNumber, originalTrace, StringComparison.Ordinal) ||
            !string.Equals(source.Entry.ReceivingParticipantEntityCode, dfi, StringComparison.Ordinal) ||
            source.Entry.Amount != original.Amount ||
            source.Entry.TransactionCode != original.TransactionCode ||
            !string.Equals(source.Entry.AccountNumber?.Trim(), original.DestinationAccountNumber?.Trim(),
                StringComparison.Ordinal) ||
            !string.Equals(originalDfi, originalTrace[..8], StringComparison.Ordinal))
            throw Conflict("El registro original no coincide con los datos de la transacción.");

        var sec = source.Batch.StandardEntryClassCode?.Trim().ToUpperInvariant();
        if (sec is not ("PPD" or "CCD"))
            throw Conflict("I05 admite únicamente PPD/CCD ordinarios: CTX y otros servicios requieren homologación específica.");
        if (source.Batch.CompanyName is null or { Length: 0 } ||
            source.Batch.CompanyName.Length > 16 ||
            string.IsNullOrWhiteSpace(source.Batch.CompanyId) ||
            source.Batch.CompanyId.Length > 10 ||
            string.IsNullOrWhiteSpace(source.Entry.RecipUserName) ||
            source.Entry.RecipUserName.Length > 22 ||
            string.IsNullOrWhiteSpace(source.Entry.AccountNumber) ||
            source.Entry.AccountNumber.Length > 17 ||
            original.Amount <= 0 || decimal.Round(original.Amount, 2) != original.Amount)
            throw Conflict("La información física original está incompleta o fuera de límites NACHA-M.");
        var returnCode = ReturnCode(source.Entry.TransactionCode);
        var serviceClass = returnCode is "21" or "31" or "51" ? "220" : "225";

        // El contador compartido permite trazas únicas por DFI/fecha entre flujos;
        // este incremento NO crea ni marca una devolución bancaria.
        var sequenceDate = DateOnly.FromDateTime(p.SuggestedValueDate.Date);
        var allocated = await sequences.ReserveRangeAsync(dfi, sequenceDate, 1, nowUtc, ct);
        var newTrace = dfi + allocated.StartValue.ToString("0000000", CultureInfo.InvariantCulture);
        var checkDigit = Cfa.ACHInterbank.Application.Helpers.DigitoChequeo.DigitoChequeo
            .CalcularDigitoChequeo(originalDfi).ToString(CultureInfo.InvariantCulture);
        var entry = new NachaReturnOutEntry(
            original.Id, returnCode, originalDfi, checkDigit,
            source.Entry.AccountNumber, original.Amount,
            source.Entry.RecipIdNumber ?? string.Empty,
            source.Entry.RecipUserName, source.Entry.DiscreData ?? string.Empty,
            newTrace, p.ReasonCode, originalTrace, string.Empty, dfi,
            string.Empty, newTrace);
        var batch = new NachaReturnOutBatch(
            serviceClass, source.Batch.CompanyName,
            source.Batch.DiscretionaryData ?? string.Empty,
            source.Batch.CompanyId, sec, "RETURN",
            p.OriginalValueDate.Date, p.SuggestedValueDate.Date, string.Empty,
            dfi, 1, [entry]);
        // FileIdModifier 'A' sólo dentro de un documento local NO TRANSMISIBLE;
        // asignación de nombre/ID externo único pertenece a I06 y no ocurre aquí.
        var draft = new NachaReturnOutBuildRequest(
            nowUtc, "A", source.Header.ImmediateOrigin!, source.Header.ImmediateDestination!,
            source.Header.ImmediateOriginName ?? string.Empty,
            source.Header.ImmediateDestinationName ?? string.Empty,
            "RETURN", [batch], PersistAudit: false,
            ClearingHouseCode: RegulatoryCycleScheduleCatalog.CenitCode,
            ClearingHouseName: "CENIT", NormativeVersion: CenitReturnOut2026Layout.NormativeVersion);
        var artifact = await builder.BuildReturnOutAsync(draft, ct);
        VerifyArtifact(artifact, returnCode, originalTrace, p.ReasonCode, newTrace, draft);
        var content = Encoding.ASCII.GetBytes(artifact.Content);
        var sha = Convert.ToHexString(SHA256.HashData(content));
        var name = $"NO_TRANSMITIR_CENIT_I05_{p.Id:N}.txt";
        p.ProtectedArtifactContent = _protector.Protect(artifact.Content);
        p.ArtifactSha256 = sha;
        p.ArtifactFileName = name;
        p.ArtifactTraceNumber = newTrace;
        p.ArtifactProfileCode = artifact.ProfileCode;
        p.ArtifactRecordCount = artifact.RecordCount;
        p.GeneratedAtUtc = nowUtc;
        p.GeneratedByUserId = actor;
        p.Status = Generated;
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Conflict("Otra operación modificó la preparación."); }
        return new CenitReturnArtifactResult(p.Id, name, content, sha,
            artifact.RecordCount, artifact.ProfileCode);
    }

    /// <summary>La descarga es local/manual, requiere misma puerta de autorización y ventana vigente.</summary>
    public async Task<CenitReturnArtifactResult> ReadLocalAsync(Guid preparationId,
        CancellationToken ct = default)
    {
        var p = await LoadPreparationAsync(preparationId, ct);
        if (p.Status != Generated || p.ProtectedArtifactContent is null ||
            p.ArtifactSha256 is null || p.ArtifactFileName is null ||
            p.ArtifactProfileCode != CenitReturnOut2026Layout.ProfileCode ||
            !p.GeneratedAtUtc.HasValue)
            throw Conflict("No existe un artefacto CENIT local íntegro disponible.");
        try { await new CenitReturnReviewService(context, clock).ValidateApprovalAsync(p, ct); }
        catch (CenitReturnReviewConflictException ex) { throw Conflict(ex.Message); }
        string plaintext;
        try { plaintext = _protector.Unprotect(p.ProtectedArtifactContent); }
        catch (CryptographicException) { throw Conflict("No se pudo verificar el cifrado del artefacto."); }
        var data = Encoding.ASCII.GetBytes(plaintext);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(data)), p.ArtifactSha256,
                StringComparison.Ordinal) ||
            data.Length % CenitReturnOut2026Layout.RecordLength != 0 || data.Length == 0)
            throw Conflict("La huella del artefacto no coincide con el original cifrado.");
        return new CenitReturnArtifactResult(p.Id, p.ArtifactFileName, data,
            p.ArtifactSha256, p.ArtifactRecordCount ?? 0, p.ArtifactProfileCode);
    }

    public async Task<CenitReturnArtifactMetadata> GetMetadataAsync(Guid preparationId,
        CancellationToken ct = default)
    {
        var p = await LoadPreparationAsync(preparationId, ct);
        return new CenitReturnArtifactMetadata(p.Id, p.OriginalTransactionId, p.Status,
            p.ArtifactSha256, p.ArtifactFileName, p.ArtifactProfileCode,
            p.ArtifactTraceNumber, p.GeneratedAtUtc, p.GeneratedByUserId);
    }

    private async Task<CenitReturnPreparation> LoadPreparationAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw Conflict("Identificador inválido.");
        var p = await context.CenitReturnPreparations
            .Include(x => x.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (p?.OriginalTransaction?.AchCycle?.ClearingHouse?.Code != RegulatoryCycleScheduleCatalog.CenitCode)
            throw Conflict("No existe preparación CENIT para el identificador solicitado.");
        return p;
    }

    private async Task<RawSource> LoadUniqueRawSourceAsync(CenitReturnPreparation p,
        int houseId, CancellationToken ct)
    {
        var links = await context.IncomingNachaTransactionLinks.AsNoTracking()
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
            .Include(x => x.EntryDetail).ThenInclude(x => x!.NachaHeader)
            .Where(x => x.IsFinal && x.AchTransactionId == p.OriginalTransactionId)
            .Take(2).ToListAsync(ct);
        if (links.Count != 1 || links[0].EntryDetail?.BatchHeader is null ||
            links[0].EntryDetail.NachaHeader is null ||
            links[0].Ingestion?.ResolvedClearingHouseId != houseId ||
            links[0].EntryDetail.NachaHeader.ClearingHouseId != houseId ||
            links[0].LinkType is not (IncomingNachaLinkType.ExactTrace15 or IncomingNachaLinkType.ExactCompositeBusinessKey))
            throw Conflict("Se requiere un único vínculo final NACHA-M CENIT 1/5/6 confirmado.");
        var original = links[0].EntryDetail;
        var header = original.NachaHeader;
        if (!OnlyDigits(header.ImmediateOrigin, 10) ||
            !OnlyDigits(header.ImmediateDestination, 10))
            throw Conflict("Encabezado original CENIT sin identificadores verificables.");
        return new RawSource(original, original.BatchHeader, header);
    }

    internal static void VerifyArtifact(NachaReturnOutBuildResult result,
        string expectedReturnCode, string originalTrace, string reason, string newTrace,
        NachaReturnOutBuildRequest? request = null)
    {
        if (result.LegacyFallbackUsed || result.ProfileCode != CenitReturnOut2026Layout.ProfileCode ||
            result.NormativeVersion != CenitReturnOut2026Layout.NormativeVersion ||
            string.IsNullOrEmpty(result.Content) || result.Content.Length % 106 != 0 ||
            result.Content.Any(c => c > 127 || c is '\n' or '\r') ||
            result.RecordCount < 6 || result.Content.Length / 106 != result.RecordCount)
            throw Conflict("El motor no produjo un artefacto del perfil oficial CENIT 2026.");
        var records = Enumerable.Range(0, result.RecordCount)
            .Select(i => result.Content.Substring(i * 106, 106)).ToArray();
        if (records.Length != 10 ||
            !records.Take(6).Select(r => r[0]).SequenceEqual(new[] { '1', '5', '6', '7', '8', '9' }) ||
            records.Skip(6).Any(r => r.Any(c => c != '9')) ||
            records[2].Substring(1, 2) != expectedReturnCode ||
            records[2].Substring(87, 15) != newTrace ||
            records[3].Substring(1, 2) != "99" ||
            records[3].Substring(3, 3) != reason ||
            records[3].Substring(6, 15) != originalTrace ||
            records[3].Substring(81, 15) != newTrace)
            throw Conflict("Los registros NACHA-M de devolución no concuerdan con su evidencia fuente.");
        if (request is not null)
        {
            var data = request.Batches.Single().Entries.Single();
            var expectedAmount = checked((long)(data.Amount * 100m))
                .ToString("D18", CultureInfo.InvariantCulture);
            if (records[0].Substring(3, 10) != request.ImmediateDestination ||
                records[0].Substring(13, 10) != request.ImmediateOrigin ||
                records[1].Substring(50, 3) != request.Batches.Single().StandardEntryClassCode ||
                records[1].Substring(71, 8) != request.Batches.Single()
                    .EffectiveEntryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ||
                records[2].Substring(3, 8) != data.ReceivingDfi ||
                records[2].Substring(12, 17).TrimEnd() != data.AccountNumber ||
                records[2].Substring(29, 18) != expectedAmount ||
                records[2].Substring(86, 1) != "1" ||
                records[3].Substring(29, 8) != data.OriginalReceivingDfi ||
                records[4].Substring(4, 6) != "000002" ||
                records[5].Substring(13, 8) != "00000002")
                throw Conflict("Los campos físicos NACHA-M 1/5/6/7/8/9 no coinciden con el original.");
        }
    }

    private static string ReturnCode(string? originalCode) => originalCode switch
    {
        "21" or "22" or "23" => "21", "31" or "32" or "33" => "31",
        "51" or "52" or "53" => "51", "26" or "27" or "28" => "26",
        "36" or "37" or "38" => "36", "55" or "56" or "57" => "56",
        _ => throw Conflict("Código transaccional CENIT no admitido para devolución ordinaria I05.")
    };
    private static string RequiredDigits(string? value, int size, string field) =>
        OnlyDigits(value, size) ? value! : throw Conflict($"{field} inválido.");
    private static bool OnlyDigits(string? value, int size) =>
        value is not null && value.Length == size && value.All(c => c is >= '0' and <= '9');
    private static bool IsValidActor(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        !string.Equals(value, "system", StringComparison.OrdinalIgnoreCase);
    private static CenitReturnArtifactConflictException Conflict(string message) => new(message);
    private sealed record RawSource(EntryDetail Entry, BatchHeader Batch, NachaHeader Header);
}
