#!/usr/bin/env python3
"""Static source regression guards for ACH Colombia MFT disabled-flow UX.

This is not a .NET/Angular build and does not exercise a bank connection.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / 'src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs'
SPA = ROOT / 'web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange'
TESTS = ROOT / 'tests/Cfa.ACHInterbank.Tests'

api = API.read_text()
ts = (SPA / 'ach-colombia-file-exchange.component.ts').read_text()
html = (SPA / 'ach-colombia-file-exchange.component.html').read_text()
scss = (SPA / 'ach-colombia-file-exchange.component.scss').read_text()
spatest = (SPA / 'ach-colombia-file-exchange.component.spec.ts').read_text()
backendtests = (TESTS / 'AchColombiaManagedMftCompositionTests.cs').read_text()
openapi = (TESTS / 'OpenApiDocumentGenerationTests.cs').read_text()
checks = {
    'API only maps explicitly known disabled code': api.count('catch (InvalidOperationException ex) when (ex.Message == "ACHCOL_MFT_DISABLED")') == 3,
    'API returns HTTP 409': 'Status = StatusCodes.Status409Conflict' in api and 'return Conflict(problem)' in api,
    'API response code stable': 'problem.Extensions["code"] = "ACHCOL_MFT_DISABLED"' in api,
    'API description never reflects exception': 'Detail = "La integración de archivos de ACH Colombia está deshabilitada.' in api,
    'Manual inbound route preserved': '[HttpPost("inbound/execute")]' in api and 'ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct)' in api,
    'Manual outbound route preserved': '[HttpPost("outbound/execute")]' in api and 'ExecuteOutboundAsync(request.CycleId, AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct, request.CorrectedFromTransferId)' in api,
    'Retry route preserved': '[HttpPost("transfers/{id:guid}/retry")]' in api and 'operationsService.RetryAsync(id, Actor(), Idempotency(), ct)' in api,
    'All three 409 OpenAPI annotations': api.count('[OpenApiResponseDescription(StatusCodes.Status409Conflict, "El intercambio administrado ACH Colombia está deshabilitado;') == 3,
    'OpenAPI contract tests include all three 409': openapi.count('El intercambio administrado ACH Colombia está deshabilitado;') >= 3,
    'SPA maps conflict only by code/status': "error.status === 409" in ts and "error.error?.code === 'ACHCOL_MFT_DISABLED'" in ts,
    'SPA generic errors stay generic': 'No fue posible completar la operación. Revisa el estado y vuelve a intentar.' in ts,
    'SPA clears disabled state before each operation': 'this.mftDisabled = false;' in ts,
    'SPA retains outbound, inbound and retry methods': all(x in ts for x in ('executeOutbound(): void', 'executeInbound(): void', 'retry(): void')),
    'SPA alert uses Material': '<mat-card *ngIf="mftDisabled"' in html and '<mat-icon aria-hidden="true">info</mat-icon>' in html,
    'SPA alert accessible': 'class="mft-alert" role="alert"' in html,
    'Settings link shown conditionally': '*ngIf="canViewMftSettings"' in html and 'routerLink="/administracion/mft-ach"' in html,
    'Settings link checks current auth': "this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'])" in ts,
    'No raw exception in banner': '{{ error }}' in html and 'ACHCOL_MFT_DISABLED' not in html,
    'SPA uses semantic warning token': 'var(--color-warning-surface)' in scss and 'var(--color-warning-text)' in scss,
    'SPA tests known and unknown errors': 'OTHER_CONFLICT' in spatest and 'ACHCOL_MFT_DISABLED' in spatest and 'status: 500' in spatest,
    'Backend tests preserve unexpected failures': 'ExecuteInbound_UnexpectedException_StillPropagatesForDiagnostics' in backendtests,
    'Backend tests cover inbound/outbound/retry': all(x in backendtests for x in ('ExecuteInbound_DisabledProfile_ReturnsControlledConflictWithoutLeakingException', 'ExecuteOutbound_DisabledProfile_ReturnsSameControlledConflict', 'Retry_DisabledProfile_ReturnsSameControlledConflict')),
}
for name, passed in checks.items():
    print(f"{'PASS' if passed else 'FAIL'} | {name}")
failed = [name for name, passed in checks.items() if not passed]
print(f'{len(checks) - len(failed)}/{len(checks)} static checks passed')
raise SystemExit(1 if failed else 0)
