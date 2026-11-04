"""I06-ACH: chequeos de estructura y aislamiento, NO sustituyen compilación ni UAT.
Uso: python scripts/test/automatic_returns_i06_ach_static_check.py [raiz_del_proyecto]
"""
from pathlib import Path
import json, sys
R=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[2]
def s(p): return (R/p).read_text(encoding='utf-8')
settings=s('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnSettingsService.cs')
runner=s('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnFileRunner.cs')
review=s('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs')
pipe=s('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs')
orch=s('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs')
api=s('src/Cfa.ACHInterbank.Api/Controllers/AchColombiaReturnReviewController.cs')
ui=s('web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.html')
config=json.loads(s('src/Cfa.ACHInterbank.Api/appsettings.json'))
tests=s('tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs')
checks={
 'Puerta técnica desactivada inicialmente': config['AchCol']['EnableAutomaticReturnFileGeneration'] is False,
 'Doble condición informada en configuración': 'mode == Automatic' in settings and 'EnableAutomaticReturnFileGeneration' in settings,
 'Sin cron nuevo en runner': 'Quartz' not in runner and 'IHostedService' not in runner,
 'Límite estricto por pasada': 'Math.Clamp(maxItems, 1, 10)' in runner,
 'Ruta exclusiva ACH Colombia': 'RegulatoryCycleScheduleCatalog.AchColombiaCode' in runner and 'RegulatoryCycleScheduleCatalog.CenitCode' not in runner,
 'Solo preparadas o aprobadas': 'AchColombiaReturnPreparationService.Prepared' in runner and 'AchColombiaReturnReviewService.Approved' in runner,
 'Consultas sin alterar resultados core': 'review.GenerateAutomaticallyAsync(id, ct)' in runner and 'core' in runner,
 'Apagado entre candidatas': runner.count('IsAutomaticModeAsync(ct)') >= 2,
 'Detención al fallar': 'catch (Exception ex)' in runner and 'break;' in runner,
 'Puerta integrada a orquestador existente': 'allowAutomaticReturnFileGeneration:' in orch,
 'Falla cerrada cuando no existe el servicio': 'returnReviewService is not null' in pipe,
 'Lanzamiento tras persistencia de resultados': 'await _achColReturnPreparation.PreparePendingAsync' in pipe and 'await _achColAutomaticGenerator.RunAsync' in pipe,
 'Acción interna sin ruta API de generación automática': 'GenerateAutomaticallyAsync' not in api,
 'Método automático separado del manual': 'public Task<GenerateReturnsFileResponse> GenerateAutomaticallyAsync' in review and 'public Task<GenerateReturnsFileResponse> GenerateApprovedFileAsync' in review,
 'Puerta revisada dentro del servicio de generación': 'if (automatic && configuration?.GetValue<bool>' in review,
 'Modo AUTOMATICO revalidado en el servicio': 'requireAutomaticMode: automatic' in review and 'mode != AchColombiaAutomaticReturnSettingsService.Automatic' in review,
 'Ningún salto de R10': 'candidate.ReasonCode == "R10"' in review,
 'Ventana revalidada': 'AchColombiaReturnPreparationWindow.Evaluate' in review,
 'No duplicar manual': 'context.AchReturnsGenerated.AsNoTracking()' in review and 'AnyAsync(g => g.OriginalTransactionId' in review,
 'Archivo confirmado persistido': 'generated.FileName' in review and 'response.TotalReturns != 1' in review,
 'Estado sin afirmación de transmisión': 'candidate.Status = FileGenerated' in review and 'ARCHIVO_GENERADO_SIN_ENVIO' in review,
 'Recuperación no invoca generador': 'await artifactService.BuildAsync(generated.FileName, ct)' in review and 'RecoverGeneratedFileAsync' in review,
 'Recuperación valida identidad y cámara': 'artifact.ClearingHouseId != candidate.OriginalTransaction.AchCycle.ClearingHouseId' in review and 'artifact.TransactionIds.Count != 1' in review,
 'API protegida reutiliza permiso': 'P0Policies.ReturnsGenerateFile' in api and 'HttpGet("{candidateId:long}/archivo")' in api,
 'SPA conserva material y vista existente': 'mat-stroked-button' in ui and 'Descargar archivo existente' in ui,
 'Pruebas de desactivación y CENIT': 'DeploymentGateDefaultsOff' in tests and 'CenitCannotEnterAutomaticAchGeneration' in tests,
 'Pruebas de unicidad y recuperación': 'PreparedCandidateGeneratesOnlyOnce' in tests and 'ExistingFileCanBeRecovered' in tests,
 'No se invoca transporte automático': 'IAchOutboundReturnDispatchService' not in runner and 'DispatchAsync(' not in runner and 'SendAsync(' not in runner,
}
failed=[]
for name,ok in checks.items():
 print(('PASS_STATIC' if ok else 'FAIL')+' | '+name)
 if not ok: failed.append(name)
print(f'RESULTADO: {len(checks)-len(failed)}/{len(checks)} comprobaciones estáticas')
if failed: sys.exit(1)
