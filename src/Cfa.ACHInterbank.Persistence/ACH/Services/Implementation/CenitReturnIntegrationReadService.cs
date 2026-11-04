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

/// <summary>
/// I06-CENIT: puente de LECTURA hacia evidencias existentes (archivo local cifrado,
/// devoluciones manuales, exportaciones y respuestas de CENIT). Por contrato el
/// artefacto NO_TRANSMITIR de I05 nunca se atribuye a un acuse de otra ruta.
/// No se crea AchFileExport, AchReturnGenerated, resultado financiero ni envío.
/// </summary>
public sealed class CenitReturnIntegrationReadService(
    AchDbContext context, IDataProtectionProvider protection, TimeProvider clock)
    : ICenitReturnIntegrationReadService
{
    private readonly IDataProtector _protector = protection.CreateProtector(
        "Cfa.ACHInterbank.CENIT.Returns.I05.LocalArtifact.v1");

    public async Task<CenitReturnIntegrationDiagnostic?> GetAsync(Guid preparationId,
        CancellationToken ct = default)
    {
        if (preparationId == Guid.Empty) return null;
        var row = await CENITPreparations().SingleOrDefaultAsync(x => x.Id == preparationId, ct);
        return row is null ? null : await InspectAsync(row, ct);
    }

    public async Task<CenitReturnIntegrationPage> ListAsync(int maxItems = 100,
        CancellationToken ct = default)
    {
        if (maxItems is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxItems), "El límite debe estar entre 1 y 100.");
        // Tomar uno adicional para informar truncamiento sin inferir el tamaño del histórico.
        var rows = await CENITPreparations()
            .OrderByDescending(x => x.PreparedAtUtc).ThenByDescending(x => x.Id)
            .Take(maxItems + 1).ToListAsync(ct);
        var results = new List<CenitReturnIntegrationDiagnostic>(Math.Min(rows.Count, maxItems));
        foreach (var row in rows.Take(maxItems))
            results.Add(await InspectAsync(row, ct));
        return new CenitReturnIntegrationPage(results, rows.Count > maxItems, results.Count);
    }

    private IQueryable<CenitReturnPreparation> CENITPreparations() =>
        context.CenitReturnPreparations.AsNoTracking()
            .Where(x => x.OriginalTransaction.AchCycle.ClearingHouse!.Code
                == RegulatoryCycleScheduleCatalog.CenitCode)
            .Include(x => x.OriginalTransaction).ThenInclude(t => t.AchCycle);

    private async Task<CenitReturnIntegrationDiagnostic> InspectAsync(
        CenitReturnPreparation p, CancellationToken ct)
    {
        // Sólo CENIT, incluso en almacenes que contienen registros de ambas cámaras.
        var houseId = p.OriginalTransaction.AchCycle.ClearingHouseId;
        var config = await context.ClearingHouseConfigs.AsNoTracking()
            .Where(x => x.ClearingHouseId == houseId)
            .Select(x => x.AutomaticReturnMode).SingleOrDefaultAsync(ct);
        var mode = config ?? "DESACTIVADO";
        var alerts = new List<string> { "SIN_NOMBRE_EXTERNO_HOMOLOGADO",
            "SIN_TRANSPORTE_I06", "SIN_ACUSE_ATRIBUIBLE_I05", "SIN_CONCILIACION_FINANCIERA" };
        if (mode == "DESACTIVADO") alerts.Add("MODO_CENIT_DESACTIVADO");

        var integrity = InspectLocalArtifact(p);
        if (integrity == "NO_GENERADO") alerts.Add("SIN_ARTEFACTO_LOCAL");
        else if (integrity != "INTEGRO_LOCAL") alerts.Add("ARTEFACTO_LOCAL_INCONSISTENTE");

        // Esta tabla corresponde a la ruta tradicional de devoluciones. No es una
        // evidencia de que el artefacto I05 haya salido de la aplicación.
        var manualReturns = await context.AchReturnsGenerated.AsNoTracking()
            .CountAsync(x => x.OriginalTransactionId == p.OriginalTransactionId, ct);
        if (manualReturns > 0) alerts.Add("DEVOLUCION_EXISTENTE_RUTA_MANUAL");

        // Una misma traza en una exportación CENIT distinta puede señalar
        // colisión. Incluso la coincidencia NO atribuye la exportación a I05.
        var traceExports = string.IsNullOrWhiteSpace(p.ArtifactTraceNumber) ? 0 :
            await context.AchFileExportTransactions.AsNoTracking().CountAsync(x =>
                x.AchFileExport.ClearingHouseId == houseId &&
                x.TraceNumber == p.ArtifactTraceNumber, ct);
        if (traceExports > 0) alerts.Add("TRAZA_LOCAL_PRESENTE_EN_OTRA_EXPORTACION");

        // Las respuestas externas pueden referir a la transacción/trace de otros
        // procesos. No existe AchFileExportId para el artefacto I05: NUNCA se
        // interpretan ACK, NACK ni aceptación como acuse de esta preparación.
        var responses = await context.CenitChamberResponses.AsNoTracking().CountAsync(x =>
            x.ClearingHouseId == houseId &&
            (x.AchTransactionId == p.OriginalTransactionId ||
             (p.ArtifactTraceNumber != null &&
                 x.TransactionTraceNumber == p.ArtifactTraceNumber)), ct);
        if (responses > 0) alerts.Add("RESPUESTA_EXTERNA_NO_ATRIBUIBLE_A_I05");

        var status = manualReturns > 0 || traceExports > 0
            ? "CONFLICTO_REQUIERE_REVISION"
            : integrity == "INTEGRO_LOCAL" ? "PENDIENTE_SIN_TRANSMISION"
            : "INCONSISTENTE_O_INCOMPLETA";
        return new CenitReturnIntegrationDiagnostic(
            p.Id, p.OriginalTransactionId, p.Status, integrity, status, mode,
            p.ArtifactSha256, manualReturns, traceExports, responses, alerts,
            clock.GetUtcNow().UtcDateTime);
    }

    private string InspectLocalArtifact(CenitReturnPreparation p)
    {
        if (p.Status != CenitReturnArtifactService.Generated &&
            p.ProtectedArtifactContent is null && p.ArtifactSha256 is null)
            return "NO_GENERADO";
        if (p.Status != CenitReturnArtifactService.Generated ||
            string.IsNullOrWhiteSpace(p.ProtectedArtifactContent) ||
            p.ArtifactSha256?.Length != 64 ||
            p.ArtifactProfileCode != CenitReturnOut2026Layout.ProfileCode ||
            p.ArtifactRecordCount != 10 || !p.GeneratedAtUtc.HasValue ||
            string.IsNullOrWhiteSpace(p.ArtifactTraceNumber) ||
            p.ArtifactTraceNumber.Length != 15 ||
            p.ArtifactFileName != $"NO_TRANSMITIR_CENIT_I05_{p.Id:N}.txt")
            return "METADATOS_INCONSISTENTES";
        try
        {
            var value = _protector.Unprotect(p.ProtectedArtifactContent);
            if (value.Length != 1060 || value.Any(c => c > 127 || c is '\r' or '\n'))
                return "ESTRUCTURA_INVALIDA";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(value)));
            if (!string.Equals(hash, p.ArtifactSha256, StringComparison.Ordinal))
                return "HASH_NO_COINCIDE";
            var records = Enumerable.Range(0, 10).Select(i => value.Substring(i * 106, 106)).ToArray();
            if (!records.Take(6).Select(x => x[0]).SequenceEqual(new[] { '1', '5', '6', '7', '8', '9' }) ||
                records.Skip(6).Any(x => x.Any(c => c != '9')) ||
                records[2].Substring(87, 15) != p.ArtifactTraceNumber ||
                records[2].Substring(29, 18) != checked((long)(p.OriginalTransaction.Amount * 100m))
                    .ToString("D18", CultureInfo.InvariantCulture) ||
                records[3].Substring(6, 15) != p.OriginalTransaction.TraceNumber ||
                records[3].Substring(1, 2) != "99" ||
                records[3].Substring(3, 3) != p.ReasonCode ||
                records[3].Substring(81, 15) != p.ArtifactTraceNumber)
                return "ESTRUCTURA_INVALIDA";
            return "INTEGRO_LOCAL";
        }
        catch (CryptographicException) { return "CLAVE_O_CIFRADO_NO_DISPONIBLE"; }
        catch (FormatException) { return "CLAVE_O_CIFRADO_NO_DISPONIBLE"; }
        catch (OverflowException) { return "ESTRUCTURA_INVALIDA"; }
    }
}
