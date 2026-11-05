#!/usr/bin/env python3
"""Dependency-free static smoke check for the NACHA upload Angular UX surface.

This is not a replacement for Angular build, unit tests or browser UAT.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
COMP = ROOT / 'web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload'
TS = (COMP / 'nacha-upload.component.ts').read_text(encoding='utf-8')
HTML = (COMP / 'nacha-upload.component.html').read_text(encoding='utf-8')
SCSS = (COMP / 'nacha-upload.component.scss').read_text(encoding='utf-8')
ROUTES = (ROOT / 'web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts').read_text(encoding='utf-8')
SERVICE = (ROOT / 'web/ach-interbank-ui/src/app/features/transactions/services/nacha-upload.service.ts').read_text(encoding='utf-8')
STYLES = (ROOT / 'web/ach-interbank-ui/src/styles.scss').read_text(encoding='utf-8')

checks = {
    'Material cards and select': all(token in HTML for token in ('<mat-card', '<mat-select', '<mat-option')),
    'Material progress and help': all(token in HTML for token in ('<mat-progress-bar', '<mat-expansion-panel')),
    'File picker and drop interactions': all(token in HTML for token in ('type="file"', '(drop)="onFileDropped($event)"', '(click)="fileInput.click()"')),
    'Accessible feedback': all(token in HTML for token in ('role="alert"', 'aria-live="polite"', 'data-testid="nacha-upload-selected-error"')),
    'Operational result and records retained': all(token in HTML for token in ('nacha-upload-result-message', 'nacha-upload-ingestion-id', 'nacha-reprocess-button', 'ui-grilla-empresarial')),
    'Existing brand tokens consumed': all(token in SCSS for token in ('--color-primary', '--color-surface', '--color-border', '--color-focus')),
    'Material branding bridge retained': '--mat-sys-on-primary: var(--brand-on-primary)' in STYLES,
    'No local hardcoded hex/rgb': not re.search(r'#[0-9a-fA-F]{3,8}\b|rgba?\(', SCSS),
    'Original route and permissions': "path: 'nacha-upload'" in ROUTES and "permissions: ['Nacha.Upload', 'CanManageAch']" in ROUTES,
    'Existing API endpoints': "resolveUrl('NachaUpload/upload')" in SERVICE and "resolveUrl('NachaUpload/records')" in SERVICE,
    'Reprocess remains explicit': 'submitUpload(true, this.lastUploadResult.ingestionId)' in TS,
}

for label, ok in checks.items():
    print(f'{"PASS" if ok else "FAIL"}: {label}')
if not all(checks.values()):
    raise SystemExit(1)
print(f'STATIC_CHECKS_PASSED={len(checks)}')
