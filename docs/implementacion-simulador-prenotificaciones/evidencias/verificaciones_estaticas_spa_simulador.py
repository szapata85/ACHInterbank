#!/usr/bin/env python3
"""Verificaciones de regresion estaticas del simulador. No sustituye pruebas .NET/Angular/E2E."""
from pathlib import Path
import json, sys
root = Path(sys.argv[1]) if len(sys.argv)>1 else Path('/mnt/data/audit_spa_workspace/ACHInterbank_ajustado')
base = root / 'web/ach-interbank-ui/src/app'
service = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs').read_text()
component = (base / 'features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts').read_text()
html = (base / 'features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html').read_text()
controller = (root / 'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs').read_text()
auth = (root / 'src/Cfa.ACHInterbank.External/DependencyInjectionService.cs').read_text()
spec = (root / 'web/ach-interbank-ui/e2e/nacha-simulator-differential-responses.spec.ts').read_text()
config = (root / 'src/Cfa.ACHInterbank.Persistence/Configuration/NachaInboundSimulationConfiguration.cs').read_text()
checks = {
    'SPA_01_descarga_ventana_sin_bearer': "window.open(this.api.downloadUrl(id)" in component and 'AddAuthentication(JwtBearerDefaults.AuthenticationScheme)' in auth,
    'SPA_02_descripcion_lote_no_normativa': '(54, ScenarioDescription(request.ScenarioType))' in service and 'Replace("Incoming", "")' in service,
    'SPA_03_monto_incoming_tomado_de_tx': 'var entryAmount = tx?.Amount ?? request.Amount;' in service and 'LoadCycleTransactionsAsync(' in service,
    'SPA_04_revalidacion_diferencial_incompleta': 'if (request.SimulationMode == NachaSimulationMode.DifferentialResponses)' in service and 'x.State is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr' in service and 'var hasPriorResponse = priorResponses.Contains' in service,
    'SPA_05_ciclo_referencias_no_correlacionadas': 'EnsureCycleAvailableAsync(request.CycleCode' in service and 'references.Contains(x.TransactionExternalId)' in service,
    'SPA_06_maximo_diferencial_no_aplicado_a_seleccion': 'request.EntriesCount > Math.Max(1, _options.MaxEntriesPerSimulation)' in service and 'Array.from(this.selectedTransactionReferences.values())' in component,
    'SPA_07_secuencia_conteo_no_atomico': 'NachaInboundSimulations.CountAsync(' in service and 'var next = count + 1;' in service and 'builder.HasIndex(x => x.FileName);' in config,
    'SPA_08_persistencia_no_atomica_archivos_db': service.find('await File.WriteAllBytesAsync(path, bytes, ct)') < service.find('await _context.SaveChangesAsync(ct)', service.find('await File.WriteAllBytesAsync(path, bytes, ct)')) < service.find('await WriteEvidenceAsync(outputDirectory'),
    'SPA_09_cambio_camara_no_limpia_referencias': 'clearingHouseChanged(): void {' in component and 'simulationContextChanged(): void {' in component and 'this.selectedTransactionIds.clear();' not in component.split('clearingHouseChanged(): void {',1)[1].split('simulationContextChanged(): void {',1)[0],
    'SPA_10_escenarios_diferenciales_bloqueados': 'DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED' in service and 'IncomingCreditConfirmation' in component,
    'SPA_11_etiqueta_uat_estatica': 'UAT LOCAL' in html and 'Enabled' in (root / 'src/Cfa.ACHInterbank.Application/ACH/Configuration/NachaInboundSimulatorOptions.cs').read_text(),
    'SPA_12_e2e_selectores_desactualizados': "button.mode-card" in spec and '<mat-card appearance="outlined" class="mode-card">' in html,
    'SPA_13_preview_camara_inexistente_error_no_controlado': 'var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);' in service and 'return Ok(await _service.PreviewAsync(request, ct));' in controller,
    'SPA_14_require_synthetic_no_aplicado': '_options.RequireSyntheticData' not in service and 'IsSynthetic = tx is null' in service,
}
output = {'type':'static_source_pattern_checks','checks':checks,'matched':sum(checks.values()),'total':len(checks),'runtimeExecuted':False,'limitations':'Las coincidencias ayudan a reproducir los hallazgos; no son pruebas funcionales ni demuestran explotabilidad.'}
print(json.dumps(output, ensure_ascii=False, indent=2))
if not all(checks.values()):sys.exit(1)
