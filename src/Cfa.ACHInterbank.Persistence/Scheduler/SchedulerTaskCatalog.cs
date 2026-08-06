namespace Cfa.ACHInterbank.Persistence.Scheduler;

internal sealed record SchedulerTaskCatalogEntry(
    string TaskCode,
    string HandlerCode,
    string Name,
    string Description,
    string Category,
    string ProcessType,
    string? SoapService,
    bool ManualAllowed,
    bool UsesCycleSchedule,
    int MinimumIntervalMinutes = 5);

internal static class SchedulerTaskCatalog
{
    private static readonly SchedulerTaskCatalogEntry[] Entries =
    [
        new(
            "ACH_CYCLE_SEED",
            "AchCycleSeeder",
            "Actualizar ciclos de compensación",
            "Verifica y actualiza la programación de los ciclos de ACH Colombia y CENIT.",
            "Configuración operativa",
            "Actualización de ciclos",
            null,
            false,
            false,
            60),
        new(
            "ACH_CYCLE_SCHEDULER",
            "AchCycleScheduler",
            "Preparar ciclos operativos",
            "Crea los ciclos operativos diarios a partir de la configuración vigente de cada cámara.",
            "Ciclos de compensación",
            "Preparación de ciclos",
            null,
            true,
            false,
            30),
        new(
            "BANK_HOLIDAY_SEED",
            "SeedBankHolidays",
            "Actualizar días festivos",
            "Mantiene actualizado el calendario de días no laborables utilizado por los procesos ACH.",
            "Calendario operativo",
            "Actualización de calendario",
            null,
            true,
            false,
            60),
        new(
            "TACIT_ACCEPTANCE",
            "AchTacitAcceptanceJob",
            "Aplicar aceptación tácita",
            "Evalúa las prenotificaciones cuyo plazo finalizó y aplica las reglas operativas vigentes.",
            "Operación ACH",
            "Aceptación tácita",
            null,
            false,
            false,
            15),
        new(
            "CONTRAPARTIDA_DISPATCH",
            "AchContrapartidasByCycle",
            "Despachar débitos originados por CFA",
            "Evalúa y envía los movimientos débito elegibles del ciclo vigente.",
            "Integración SOAP",
            "Movimiento monetario débito",
            "Proc_Contrapartidas",
            true,
            true,
            5),
        new(
            "INCOMING_NACHA_PROCESSING",
            "IncomingNachaPostProcessing",
            "Procesar créditos recibidos",
            "Evalúa entradas NACHA-M y envía únicamente los movimientos crédito elegibles.",
            "Integración SOAP",
            "Movimiento monetario crédito",
            "Proc_Transacciones",
            true,
            true,
            3),
        new(
            "ach-response-reprocess-dispatcher",
            "ach-response-reprocess-dispatcher",
            "Procesar respuestas diferenciales",
            "Procesa de forma idempotente solicitudes autorizadas de reproceso de respuestas ACH, sin movimientos monetarios.",
            "Respuestas ACH",
            "Notificación no monetaria",
            "RegistrarRespuestaTransaccion",
            true,
            false,
            1),
        new(
            "SCHEDULER_CLUSTER_PROBE",
            "SCHEDULER_CLUSTER_PROBE",
            "Verificar disponibilidad del programador",
            "Comprueba de forma controlada la coordinación, recuperación e idempotencia del programador.",
            "Diagnóstico técnico",
            "Verificación técnica",
            null,
            true,
            false,
            60)
    ];

    public static SchedulerTaskCatalogEntry? ByTaskCode(string taskCode)
        => Entries.FirstOrDefault(x => string.Equals(x.TaskCode, taskCode?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static SchedulerTaskCatalogEntry? ByHandlerCode(string handlerCode)
        => Entries.FirstOrDefault(x => string.Equals(x.HandlerCode, handlerCode?.Trim(), StringComparison.OrdinalIgnoreCase));
}
