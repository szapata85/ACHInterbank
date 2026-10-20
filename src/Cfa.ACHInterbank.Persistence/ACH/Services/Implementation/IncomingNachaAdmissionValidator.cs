using System.Globalization;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class IncomingNachaAdmissionValidator : IIncomingNachaAdmissionValidator
{
    private static readonly Regex OfficialDatePattern = new(
        @"^\d{7}\.\d{3}\.(?<date>\d{8})\.\d+(?:\.OUT)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly AchDbContext _context;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaAdmissionValidator(AchDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IncomingNachaAdmissionResult> ValidateAsync(
        IncomingNachaAdmissionRequest request,
        CancellationToken ct = default)
    {
        var headerRecord = request.Records.FirstOrDefault(x => x.Length == 106 && x[0] == '1');
        if (headerRecord is null || !request.Resolution.OperationalDate.HasValue)
        {
            return Reject(null, null, null, null,
                "HEADER_PREVIEW_INVALID",
                "No fue posible leer el encabezado del archivo",
                "El archivo no contiene un encabezado NACHA-M válido de 106 posiciones.",
                "Verifique que el archivo corresponda al formato y perfil seleccionados.");
        }

        var headerDate = DateOnly.FromDateTime(request.Resolution.OperationalDate.Value);
        var fileNameDate = ParseFileNameDate(request.FileName);
        var effectiveDate = ParseEffectiveDate(request.Records);
        var cycleNumber = CenitOfficialFileNameParser.ExtractCycleNumberFromFileName(request.FileName);
        var preview = new NachaHeaderPreview(
            headerRecord.Substring(3, 10).Trim(),
            headerRecord.Substring(13, 10).Trim(),
            headerDate,
            ParseTime(headerRecord.Substring(31, 4)),
            effectiveDate,
            cycleNumber,
            headerRecord.Substring(94, 8).Trim());

        if (fileNameDate.HasValue && fileNameDate.Value != headerDate)
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "HEADER_DATE_MISMATCH",
                "La fecha del nombre no coincide con el encabezado",
                $"El nombre del archivo corresponde al {Format(fileNameDate.Value)}, pero el encabezado corresponde al {Format(headerDate)}.",
                "Seleccione el archivo cuyo nombre y encabezado correspondan a la misma fecha operativa.",
                Format(headerDate), Format(fileNameDate.Value));
        }

        if (effectiveDate.HasValue && effectiveDate.Value != headerDate)
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "EFFECTIVE_DATE_MISMATCH",
                "La fecha efectiva no coincide con la fecha operativa",
                $"El encabezado corresponde al {Format(headerDate)}, pero el primer lote tiene fecha efectiva {Format(effectiveDate.Value)}.",
                "Verifique la fecha efectiva de los lotes antes de volver a cargar el archivo.",
                Format(headerDate), Format(effectiveDate.Value));
        }

        if (!request.Resolution.ClearingHouseId.HasValue || string.IsNullOrWhiteSpace(request.Resolution.AchCycleId))
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "CYCLE_NOT_RESOLVED",
                "No fue posible identificar el ciclo operativo",
                "No existe una relación inequívoca entre la cámara, la fecha y el ciclo del archivo.",
                "Revise la cámara seleccionada, la fecha y el nombre oficial del archivo.");
        }

        var clearingHouseId = request.Resolution.ClearingHouseId.Value;
        var isHoliday = headerDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            || await _context.BankHolidays.AsNoTracking().AnyAsync(x => x.Date == headerDate, ct)
            || await _context.ClearingHouseSpecialDates.AsNoTracking()
                .AnyAsync(x => x.ClearingHouseId == clearingHouseId && x.Date == headerDate && x.IsActive, ct);
        if (isHoliday)
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "NON_BUSINESS_DAY",
                "La fecha del archivo no es un día hábil",
                $"La fecha {Format(headerDate)} no está habilitada como día operativo para la cámara seleccionada.",
                "Seleccione un archivo correspondiente a un día hábil habilitado.");
        }

        var cycle = await _context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
            .ThenInclude(x => x!.ClearingHouseConfig)
            .SingleOrDefaultAsync(x => x.Id == request.Resolution.AchCycleId
                && x.ClearingHouseId == clearingHouseId
                && x.ProcessingDate.Date == request.Resolution.OperationalDate.Value.Date, ct);
        if (cycle is null)
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "CYCLE_NOT_CONFIGURED",
                "El ciclo del archivo no está configurado",
                $"No existe un ciclo configurado para la fecha operativa {Format(headerDate)} y la cámara identificada.",
                "Solicite la configuración del ciclo o seleccione el archivo del ciclo vigente.");
        }

        if (cycle.OperationalStatus is AchCycleOperationalStatus.Closed or AchCycleOperationalStatus.Cancelled
            && !(request.IsExplicitReprocess && cycle.AllowsExplicitReprocessing))
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "CYCLE_ALREADY_CLOSED",
                "El ciclo del archivo ya está cerrado",
                $"El archivo corresponde a {cycle.CycleName}, pero ese ciclo se encuentra {StatusText(cycle.OperationalStatus)}.",
                "Seleccione el archivo correspondiente al ciclo vigente o solicite un reprocesamiento autorizado.");
        }

        var timeZone = ResolveTimeZone(cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), timeZone);
        if (DateOnly.FromDateTime(cycle.ProcessingDate) == DateOnly.FromDateTime(localNow.DateTime)
            && localNow.TimeOfDay > cycle.EndTime.Add(TimeSpan.FromMinutes(cycle.ReceptionToleranceMinutes))
            && !(request.IsExplicitReprocess && cycle.AllowsExplicitReprocessing))
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "CYCLE_RECEPTION_WINDOW_EXPIRED",
                "La ventana de recepción del ciclo finalizó",
                $"La hora límite autorizada para {cycle.CycleName} ya finalizó en la zona horaria {timeZone.Id}.",
                "Seleccione el archivo del ciclo vigente o solicite un reprocesamiento autorizado.");
        }

        var currentOpenDate = await _context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId && x.OperationalStatus == AchCycleOperationalStatus.Open)
            .Where(x => x.ProcessingDate.Date == localNow.Date)
            .Select(x => (DateTime?)x.ProcessingDate)
            .FirstOrDefaultAsync(ct);
        if (currentOpenDate.HasValue && DateOnly.FromDateTime(currentOpenDate.Value) != headerDate
            && !(request.IsExplicitReprocess && cycle.AllowsExplicitReprocessing))
        {
            return Reject(preview, fileNameDate, effectiveDate, cycleNumber,
                "OPERATIONAL_DATE_MISMATCH",
                "La fecha del archivo no corresponde a la fecha operativa habilitada",
                $"El archivo corresponde al {Format(headerDate)}, pero la fecha operativa habilitada es {Format(DateOnly.FromDateTime(currentOpenDate.Value))}.",
                "Seleccione el archivo correspondiente a la fecha operativa habilitada.",
                Format(DateOnly.FromDateTime(currentOpenDate.Value)), Format(headerDate));
        }

        return IncomingNachaAdmissionResult.Accepted(preview, fileNameDate, effectiveDate, cycleNumber);
    }

    private static DateOnly? ParseFileNameDate(string fileName)
    {
        var match = OfficialDatePattern.Match(Path.GetFileName(fileName));
        return match.Success
            && DateOnly.TryParseExact(match.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? value
                : null;
    }

    private static DateOnly? ParseEffectiveDate(IEnumerable<string> records)
    {
        var record = records.FirstOrDefault(x => x.Length == 106 && x[0] == '5');
        if (record is null) return null;
        var raw = AchColOfficialNachaLayout.Read(record, "5", "EFFECTIVEENTRYDATE").Trim();
        return DateOnly.TryParseExact(raw, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;
    }

    private static TimeOnly? ParseTime(string value)
        => TimeOnly.TryParseExact(value, "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : null;

    private static IncomingNachaAdmissionResult Reject(
        NachaHeaderPreview? header,
        DateOnly? fileNameDate,
        DateOnly? effectiveDate,
        int? cycleNumber,
        string code,
        string title,
        string message,
        string action,
        string? expected = null,
        string? found = null)
        => IncomingNachaAdmissionResult.Rejected(header, fileNameDate, effectiveDate, cycleNumber,
            new IncomingNachaAdmissionIssue(code, title, message, action, "Functional", "Error", expected, found));

    private static string Format(DateOnly value) => value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CO"));

    private static string StatusText(AchCycleOperationalStatus status) => status switch
    {
        AchCycleOperationalStatus.Closed => "cerrado",
        AchCycleOperationalStatus.Cancelled => "cancelado",
        _ => "no disponible"
    };

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        var candidates = new[] { id, id == "America/Bogota" ? "SA Pacific Standard Time" : null, "America/Bogota", "SA Pacific Standard Time" };
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate!); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
