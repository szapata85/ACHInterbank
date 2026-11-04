#!/usr/bin/env python3
"""I05 ACH Colombia: verificaciones estáticas reproducibles, NO certificación funcional."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[2]
paths={
 'iface':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaReturnReviewService.cs',
 'dto':'src/Cfa.ACHInterbank.Application/ACH/Models/AchColombiaReturnReviewModels.cs',
 'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs',
 'api':'src/Cfa.ACHInterbank.Api/Controllers/AchColombiaReturnReviewController.cs',
 'gen':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs',
 'entity':'src/Cfa.ACHInterbank.Domain/Models/ACH/AchColombiaReturnPreparation.cs',
 'spa':'web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.ts',
 'html':'web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.html',
 'spasvc':'web/ach-interbank-ui/src/app/features/transactions/services/ach-colombia-return-review-api.service.ts',
 'xunit':'tests/Cfa.ACHInterbank.Tests/AchColombiaApprovedReturnGenerationI05Tests.cs',
 'jasmine':'web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.spec.ts',
 'jasmineapi':'web/ach-interbank-ui/src/app/features/transactions/services/ach-colombia-return-review-api.service.spec.ts'
}
s={k:(root/v).read_text(encoding='utf-8-sig') for k,v in paths.items()}
checks=[
('Ruta propia ACH Colombia', 'api/ach-colombia/devoluciones-automaticas/candidatas' in s['api']),
('El endpoint no acepta selección arbitraria de tx, causal o ciclo', 'GenerateApprovedFile(long candidateId, CancellationToken ct)' in s['api'] and 'GenerateReturnsFileRequest request' not in s['api']),
('Control de permiso de generación', bool(re.search(r'\[HttpPost\("\{candidateId:long\}/generar-archivo"\)\]\s*\[Authorize\(Policy = P0Policies.ReturnsGenerateFile\)\]',s['api']))),
('Exclusión de CENIT antes de generar', 'ClearingHouse?.Code' in s['service'] and '!= RegulatoryCycleScheduleCatalog.AchColombiaCode' in s['service']),
('Solo candidata aprobada', 'candidate.Status != Approved' in s['service']),
('Modo apagado impide generación', 'AutomaticReturnMode' in s['service'] and 'WithApproval' in s['service'] and 'Automatic' in s['service']),
('Verificación causal y cuenta documental', 'AchReturnCode' in s['service'] and 'ReturnReasons' in s['service']),
('R10 sin evidencia sigue excluida', 'candidate.ReasonCode == "R10"' in s['service']),
('Se comprueba origen funcional I02', 'AchColombiaApplicationFailureDetector.DetectedEvent' in s['service'] and 'EvidenceStillMatches' in s['service']),
('Ciclo original inmutable', 'candidate.OriginalCycleId != candidate.OriginalTransaction.AchCycleId' in s['service']),
('Se reevalúa ventana del día', 'AchColombiaReturnPreparationWindow.Evaluate' in s['service'] and 'current.Value.CycleId != candidate.SuggestedReturnCycleId' in s['service']),
('Duplicado manual comprobado', 'context.AchReturnsGenerated.AsNoTracking()' in s['service']),
('Se reutiliza generador normativo', 'returnsService.GenerateReturnsFileAsync(' in s['service']),
('Ciclo y causal salen de la candidata', 'new GenerateReturnsFileRequest(candidate.OriginalCycleId' in s['service'] and 'candidate.SuggestedReturnCycleId' in s['service']),
('Unidad de trabajo transaccional y estrategia EF', 'BeginTransactionAsync(ct)' in s['service'] and 'CommitAsync(ct)' in s['service'] and 'RollbackAsync' in s['service'] and 'CreateExecutionStrategy().ExecuteAsync' in s['service']),
('Estado distinto generado sin envío', 'ARCHIVO_GENERADO_SIN_ENVIO' in s['service']),
('Respaldo del registro generado para confirmar', 'generated is null' in s['service'] and 'generated.FileName, response.FileName' in s['service']),
('Clave única existente en generador', 'AchReturnAlreadyGeneratedException' in s['gen']),
('Concurrencia por estado', 'DbUpdateConcurrencyException' in s['service']),
('Sin invocación de transporte por I05', not re.search(r'(transportAdapter|DispatchAsync|SendAsync\(|TransmitAsync\(|GenerateAndDispatchAsync)',s['service'])),
('Formato de respuesta incluye nombre oficial', 'return Ok(file);' in s['api'] and 'response.FileName' in s['service']),
('SPA usa el nombre devuelto', 'link.download = file.fileName' in s['spa']),
('SPA requiere confirmación', "action === 'GENERAR'" in s['spa'] and 'afterClosed()' in s['spa']),
('SPA bloquea si no es elegible', '!candidate.canGenerateFile' in s['spa']),
('SPA muestra generación por separado de aprobación', 'Generar archivo' in s['html'] and "item.status === 'APROBADA_SIN_ENVIO'" in s['html']),
('Sin ruta/pantalla SPA redundante', 'app-ach-colombia-return-review' in (root/'web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html').read_text()),
('Servicio SPA llama endpoint acotado', 'generateApprovedFile(candidateId: number)' in s['spasvc'] and '/generar-archivo' in s['spasvc']),
('La SPA no llama servicios CENIT', 'CENIT' not in s['spasvc']),
('Pruebas bloqueo/no envío/estado', s['xunit'].count('[Fact]')>=8 and 'CenitCandidate' in s['xunit'] and 'RequiresApprovalBeforeGenerating' in s['xunit']),
('Pruebas de fallo de generador', 'GeneratorFailureDoesNotMarkCandidateAsFileGenerated' in s['xunit']),
('Prueba SPA de confirmación', "component.confirm(approved, 'GENERAR')" in s['jasmine']),
('Prueba contrato SPA', 'service.generateApprovedFile(7)' in s['jasmineapi']),
('Sin cambios al esquema en I05', len(list((root/'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer').glob('*I05*')))==0),
]
for name,passed in checks:
    print(f"{'PASS_STATIC' if passed else 'FAIL'} | {name}")
print(f'RESULTADO {sum(int(passed) for _,passed in checks)}/{len(checks)}')
raise SystemExit(0 if all(p for _,p in checks) else 1)
