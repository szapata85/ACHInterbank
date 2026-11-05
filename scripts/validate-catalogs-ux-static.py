#!/usr/bin/env python3
"""Check scoped /catalogs UX migration without claiming an Angular runtime build."""
from __future__ import annotations
import re
import sys
from pathlib import Path
from html.parser import HTMLParser

ROOT = Path(__file__).resolve().parents[1]
SPA = ROOT / 'web/ach-interbank-ui'
BASE = SPA / 'src/app/features/catalogs'
COMPS = BASE / 'components'

class Tags(HTMLParser):
    VOID = {'input', 'meta', 'link', 'img', 'hr', 'br'}
    def __init__(self):
        super().__init__(); self.stack: list[str] = []; self.invalid: list[str] = []
    def handle_starttag(self, tag, attrs):
        if tag not in self.VOID: self.stack.append(tag)
    def handle_endtag(self, tag):
        if tag in self.VOID: return
        if not self.stack or self.stack[-1] != tag:
            self.invalid.append(f'unexpected </{tag}> after {self.stack[-1] if self.stack else "none"}')
        else: self.stack.pop()

def checked(label: str, condition: bool):
    print(('PASS' if condition else 'FAIL') + ' — ' + label)
    return int(condition)

checks = 0
passes = 0
def verify(label: str, condition: bool):
    global checks, passes
    checks += 1; passes += checked(label, condition)

routing = (BASE / 'catalogs-routing.module.ts').read_text('utf-8')
paths = re.findall(r"\bpath:\s*'([^']*)'", routing)
expected = {'', 'financial-institutions','clearing-house-preferences','bank-holidays','clearing-house-special-dates','company-entry-descriptions','document-types','gender-types','person-types','phone-types','email-types','address-types','transaction-codes'}
verify(f'all {len(expected)} /catalogs entry routes retained', set(paths) == expected and len(paths)==len(expected))
verify('all seven type catalog paths share one admin component', routing.count(".then((m) => m.CatalogTypesAdminComponent)") == 7)
verify('legacy chamber-specific path stays a redirect, not a new editor', 'ClearingHouseSpecialDatesLegacyRedirectComponent' in routing)

for filename, count in [('catalog-types-admin.component.html',3),('company-entry-description-admin.component.html',4)]:
    html=(COMPS/filename).read_text('utf-8')
    parser=Tags();parser.feed(html)
    verify(filename+' balanced HTML elements', not parser.invalid and not parser.stack)
    verify(filename+' Material form fields', html.count('<mat-form-field') == count)
    verify(filename+' material validation, busy, states and retry', all(x in html for x in ['mat-error','mat-flat-button','mat-stroked-button','role="alert"','role="status"','(reintentar)']))
    verify(filename+' no raw input or select widgets', not re.search(r'<(input|select)(?![^>]*\bmatInput)', html) if filename.startswith('catalog-types') else ('<select' not in html and 'mat-select' in html))

for name in ['catalog-types-admin.component.ts','company-entry-description-admin.component.ts']:
    ts=(COMPS/name).read_text('utf-8')
    verify(name+' uses existing MatDialog confirmation instead of browser prompt', 'CatalogActionConfirmDialogComponent' in ts and 'window.confirm' not in ts and 'afterClosed().pipe(take(1))' in ts)
    verify(name+' preserves service/CRUD and user-safe errors', all(x in ts for x in ['this.api.list(', 'this.api.create(', 'this.api.update(', 'this.api.delete(', 'catalogErrorMessage(', 'this.saving || this.deleting']))

home=(COMPS/'catalogs-list.component.html').read_text('utf-8')
verify('index uses cards, material links, retryable data grid', all(x in home for x in ['<mat-card','mat-stroked-button','ui-grilla-empresarial','(reintentar)','[routerLink]']))
verify('index navigation respects existing management permission', '*ngIf="canManage"' in home and "hasPermission('CanManageAch')" in (COMPS/'catalogs-list.component.ts').read_text('utf-8'))
verify('untranslated grid headers removed', "headerName: 'Routing'" not in (COMPS/'financial-institutions.component.ts').read_text('utf-8') and "headerName: 'Transit'" not in (COMPS/'financial-institutions.component.ts').read_text('utf-8'))
verify('bank holidays and special dates keep Material editors', all('<mat-form-field' in (COMPS/name).read_text('utf-8') for name in ['bank-holidays.component.html','clearing-house-special-dates.component.html']))
verify('other administration forms still Material', all('<mat-form-field' in (COMPS/name).read_text('utf-8') for name in ['financial-institutions.component.html','clearing-house-preferences.component.html']))
verify('new style files use branding variables, not fixed brand colors', all(not re.search(r'#[0-9A-Fa-f]{3,8}\b', (COMPS/name).read_text('utf-8')) for name in ['catalog-editor.shared.scss','catalogs-list.component.scss']))
grid=(SPA/'src/app/shared/components/ui/ui-grilla-empresarial.component.ts').read_text('utf-8')
verify('shared grid selected/hover colors derive from tenant branding', 'color-mix(in srgb, var(--color-primary)' in grid and 'background: #e8f0ff' not in grid)
verify('legacy E2E labels aligned to updated UX', "name: 'Agregar día no hábil'" in (SPA/'e2e/audits-catalogs-material.spec.ts').read_text('utf-8'))
print(f'RESULT: {passes}/{checks} static checks passed; Angular build/browser execution NOT inferred.')
sys.exit(0 if checks == passes else 1)
