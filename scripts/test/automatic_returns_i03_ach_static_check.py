"""Static source invariants for I03-ACH. NOT a substitute for dotnet test/SQL Server."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
read = lambda path: (root / path).read_text(encoding="utf-8-sig")
service = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnPreparationService.cs")
entity = read("src/Cfa.ACHInterbank.Domain/Models/ACH/AchColombiaReturnPreparation.cs")
config = read("src/Cfa.ACHInterbank.Persistence/Configuration/AchColombiaReturnPreparationConfiguration.cs")
context = read("src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs")
migration = read("src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261102230000_AchColombiaReturnPreparationI03.cs")
snapshot = read("src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs")
pipeline = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs")
tests = read("tests/Cfa.ACHInterbank.Tests/AchColombiaReturnPreparationI03Tests.cs")
checks = [
    ("Modelo de preparación", "class AchColombiaReturnPreparation" in entity),
    ("Candidata no devuelve archivo", "FileName" not in entity and "NewSequenceNumber" not in entity),
    ("BD única por original", 'HasIndex(x => x.OriginalTransactionId)' in config and '.IsUnique().HasDatabaseName("UX_AchColReturnPreparation_Original")' in config),
    ("BD única por evento", 'HasIndex(x => x.SourceEventId)' in config and 'UX_AchColReturnPreparation_Event' in config),
    ("Contexto expone candidatas", "DbSet<AchColombiaReturnPreparation>" in context),
    ("Migración incluye índice único", 'UX_AchColReturnPreparation_Original' in migration and 'unique: true' in migration),
    ("Migración conserva origen y evento", 'FK_AchColombiaReturnPreparations_AchTransactions_OriginalTransactionId' in migration and 'FK_AchColombiaReturnPreparations_IncomingNachaProcessingEvents_SourceEventId' in migration),
    ("Migración reversible", 'DropTable(name: "AchColombiaReturnPreparations")' in migration),
    ("Snapshot actualizado", snapshot.count('modelBuilder.Entity("Cfa.ACHInterbank.Domain.Models.ACH.AchColombiaReturnPreparation"') == 2),
    ("Aislamiento ACH Colombia", 'RegulatoryCycleScheduleCatalog.AchColombiaCode' in service and 'Cenit' not in service),
    ("Sólo modo activo prepara", 'AchColombiaAutomaticReturnSettingsService.WithApproval' in service and 'AchColombiaAutomaticReturnSettingsService.Automatic' in service),
    ("Incidencia causal verificada", 'AchColombiaApplicationFailureDetector.DetectedEvent' in service and 'causeVerified' in service),
    ("Sin respuesta técnica", 'execution.IsTechnicalFailure' in service and 'IntegrationTransportStatus.Succeeded' in service),
    ("No se infiere causal de texto", 'suggestedReturnReason' in service and 'ResultDescription' not in service and 'SoapResponseDescription' not in service),
    ("Código ACH por cámara y tipo", 'context.Set<AchReturnCode>()' in service and 'x.AppliesToDebit' in service),
    ("No duplica retorno manual", 'context.AchReturnsGenerated' in service),
    ("Unicidad concurrente tratada", 'RelationalDatabaseExceptionClassifier.IsUniqueViolation' in service and 'EntityState.Detached' in service),
    ("Siete ciclos: matriz existente", 'AchColombiaSevenCycleCandidate.ReturnWindows' in service and 'CycleNumberResolver' in service),
    ("Crédito tardío a revisión", 'REVISION_TARDIA_CREDITO' in service and 'original.Type == TransactionTypeEnum.Credit' in service),
    ("No intenta generar ni enviar", 'GenerateReturnsFileAsync' not in service and 'DispatchAsync' not in service and 'SendAsync' not in service),
    ("Procesador después de persistencia", 'await _context.SaveChangesAsync(ct);\n        await TryPrepareAchColombiaReturnsAsync' in pipeline),
    ("Procesador no interrumpe core", 'catch (Exception ex) when (ex is not OperationCanceledException)' in pipeline),
    ("Pruebas tres tipos", all(k in tests for k in ['TransactionTypeEnum.Credit', 'TransactionTypeEnum.Debit', 'TransactionTypeEnum.Prenotification'])),
    ("Pruebas no envío / CENIT", 'CenitDoesNotPrepareEvenWhenModeIsAutomatic' in tests and 'Assert.Empty(db.AchReturnsGenerated)' in tests),
    ("Pruebas de ventana y unicidad", 'SevenCyclePolicyAllowsPreviousOperationalDay' in tests and 'SqliteUniqueIndexBlocksConcurrentDuplicate' in tests),
]
failures = [name for name, ok in checks if not ok]
for name, ok in checks:
    print(f"{'PASS_STATIC' if ok else 'FAIL_STATIC'}: {name}")
print(f"RESULTADO: {len(checks)-len(failures)}/{len(checks)}")
sys.exit(1 if failures else 0)
