#!/usr/bin/env python3
"""Revisión estructural I07-R. NO sustituye compilación ni UAT."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / 'web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator'
component = (UI / 'nacha-inbound-simulator.component.ts').read_text()
component_tests = (UI / 'nacha-inbound-simulator.component.spec.ts').read_text()
backend = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs').read_text()
controller = (ROOT / 'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs').read_text()
cs_tests = (ROOT / 'tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.I07R.cs').read_text()
checks = {
    'preview-version-counter': 'private pnPreviewVersion = 0;' in component,
    'invalidate-previews': 'this.pnPreviewVersion++;' in component,
    'capture-outcome': 'const outcome = this.pnOutcome;' in component,
    'preview-check-version': 'version === this.pnPreviewVersion' in component,
    'preview-check-selected-id': 'this.pnSelected?.originalPrenotificationId === selectedId' in component,
    'preview-check-camera': 'this.pnHouseCode === houseCode' in component,
    'preview-check-outcome': 'this.pnOutcome === outcome' in component,
    'preview-response-guard': 'if (!isCurrent()) { return; }' in component,
    'pn-return-is-distinct': "if (outcome === 'Returned')" in component,
    'no-auto-import': 'this.api.generatePrenotificationReturn(request)' in component and 'this.api.applyDueTacitAcceptance(request)' in component,
    'auth-blob-download': "{ responseType: 'blob' }" in (ROOT / 'web/ach-interbank-ui/src/app/features/uat/services/nacha-inbound-simulator.service.ts').read_text(),
    'integrity-before-downloaded': backend.index('var content = await File.ReadAllBytesAsync(entity.FilePath, ct);') < backend.index('entity.Status = NachaInboundSimulationStatus.Downloaded;',backend.index('GetFileAsync(')),
    'sha256-check': 'SHA256.HashData(content)' in backend and 'StringComparison.OrdinalIgnoreCase' in backend,
    'length-check': 'entity.FileSizeBytes != content.LongLength' in backend,
    'fail-closed-download': 'SIMULATOR_FILE_INTEGRITY_FAILED' in backend,
    'controlled-error-422': 'return UnprocessableEntity(ToProblem(ex.Message));' in controller[controller.index('DownloadFile('):controller.index('[HttpGet("{id:int}/evidence")]')],
    'angular-race-regression': 'I07-R: una vista previa de devolución tardía' in component_tests,
    'angular-causal-regression': 'I07-R: al cambiar causal' in component_tests,
    'two-operator-download-tests': 'InlineData("ACHCOL"' in cs_tests and 'InlineData("CENIT"' in cs_tests,
    'tampered-file-test': 'File.AppendAllTextAsync' in cs_tests and 'SIMULATOR_FILE_INTEGRITY_FAILED' in cs_tests,
}
for label, ok in checks.items():
    print(('PASS_STATIC' if ok else 'FAIL_STATIC'), label)
if not all(checks.values()):
    print(f'SUMMARY {sum(checks.values())}/{len(checks)} FAIL')
    sys.exit(1)
print(f'SUMMARY {len(checks)}/{len(checks)} PASS_STATIC; NOT_DOTNET_OR_ANGULAR_OR_UAT')
