namespace Cfa.ACHInterbank.Persistence.Scheduler;

internal sealed record SchedulerTaskCatalogEntry(
    string TaskCode,
    string HandlerCode,
    string Description,
    string? ClearingHouse,
    bool ManualAllowed);

internal static class SchedulerTaskCatalog
{
    private static readonly SchedulerTaskCatalogEntry[] Entries =
    [
        new("ACH_CYCLE_SEED", "AchCycleSeeder", "Prepara los ciclos anuales ACH y CENIT.", null, false),
        new("ACH_CYCLE_SCHEDULER", "AchCycleScheduler", "Genera los ciclos operativos diarios.", null, true),
        new("BANK_HOLIDAY_SEED", "SeedBankHolidays", "Actualiza el calendario bancario colombiano.", null, true),
        new("TACIT_ACCEPTANCE", "AchTacitAcceptanceJob", "Aplica las reglas de aceptación tácita.", "ACH Colombia", false),
        new("CONTRAPARTIDA_DISPATCH", "AchContrapartidasByCycle", "Despacha contrapartidas elegibles por ciclo y cámara.", null, false),
        new("INCOMING_NACHA_PROCESSING", "IncomingNachaPostProcessing", "Procesa entradas NACHA-M elegibles y su despacho controlado.", null, false),
        new("ach-response-reprocess-dispatcher", "ach-response-reprocess-dispatcher", "Procesa solicitudes gobernadas de reproceso de respuestas ACH.", null, true),
        new("SCHEDULER_CLUSTER_PROBE", "SCHEDULER_CLUSTER_PROBE", "Prueba técnica idempotente de clúster y recuperación.", null, true)
    ];

    public static SchedulerTaskCatalogEntry? ByTaskCode(string taskCode)
        => Entries.FirstOrDefault(x => string.Equals(x.TaskCode, taskCode?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static SchedulerTaskCatalogEntry? ByHandlerCode(string handlerCode)
        => Entries.FirstOrDefault(x => string.Equals(x.HandlerCode, handlerCode?.Trim(), StringComparison.OrdinalIgnoreCase));
}
