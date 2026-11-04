#!/usr/bin/env python3
"""Offline source invariants for NAV-UX-MFT-20261027. No SDK/browser required.

This is not a substitute for Angular/.NET/E2E runtime tests.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
UI = ROOT / 'web/ach-interbank-ui/src/app'
LAYOUT = (UI / 'layout/main-layout.component.html').read_text(encoding='utf-8')
CSS = (UI / 'layout/main-layout.component.scss').read_text(encoding='utf-8')
ROUTES = (UI / 'app-routing.module.ts').read_text(encoding='utf-8')
SEED = (ROOT / 'src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs').read_text(encoding='utf-8')
SECURITY = (ROOT / 'src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConnectionsStructuralMenuAuthorizationSeeder.cs').read_text(encoding='utf-8')
MFT = (ROOT / 'src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/AchColombiaMftAdministrationMenuSeeder.cs').read_text(encoding='utf-8')
TEST = (UI / 'layout/main-layout.component.spec.ts').read_text(encoding='utf-8')
COMPONENT = (UI / 'layout/main-layout.component.ts').read_text(encoding='utf-8')
GLOBAL_BUTTONS = (ROOT / 'web/ach-interbank-ui/src/styles/_components.scss').read_text(encoding='utf-8')
HOVER_E2E = (ROOT / 'web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts').read_text(encoding='utf-8')

checks = {
    'single disclosure implementation': LAYOUT.count('(click)="toggleSubmenu(item)"') == 1,
    'one branch row button': LAYOUT.count('class="nav-item nav-parent"') == 1,
    'branch button data IDs': '[attr.data-menu-item-id]="item.id"' in LAYOUT and '[attr.data-menu-toggle-id]="item.id"' in LAYOUT,
    'explicit disclosure state': '[attr.aria-expanded]="isItemExpanded(item)"' in LAYOUT and '[attr.aria-controls]="\'submenu-\' + item.id"' in LAYOUT,
    'optional functional parent destination': '*ngIf="isNavigableParent(item)"' in LAYOUT and '[attr.data-menu-destination-id]="item.id"' in LAYOUT,
    'leaf navigation remains': '<ng-template #navigableMenuItem>' in LAYOUT,
    'mobile/desktop navigation retained': '(click)="toggleNavigation()"' in LAYOUT and '[opened]="drawerOpened"' in LAYOUT,
    'consistent destination styling': '.nav-parent-destination:focus-visible' in CSS,
    'MFT SPA route unchanged': "path: 'administracion/mft-ach'" in ROUTES,
    'MFT OR read guard retained': "permissions: ['Config.Read', 'CanReadAch']" in ROUTES,
    'MFT menu has explicit label': '"Administración de transferencias de archivos (MFT)"' in SEED,
    'MFT in connections group once': SEED.count('FileExchangeConfigurationRoute,') == 1 and 'connections.Id,\n            order: 40' in SEED,
    'MFT seeded for new databases': 'item.Label = "Administración de transferencias de archivos (MFT)"' in MFT,
    'connections authorization retained': 'context.MenuItemRoles.RemoveRange(parentRoleLinks)' in SECURITY and 'context.MenuItemRoles.RemoveRange(mftRoleLinks)' in SECURITY,
    'angular structural/functional tests added': 'uses the same whole-row disclosure control for structural and functional parents' in TEST,
    'MFT menu destination asserted': "expect(mft.getAttribute('href')).toContain('/administracion/mft-ach');" in TEST,
    'ARIA expanded reflects visible compact branch':
        'return this.expandedItems.has(item.id) && (this.isMobileViewport || !this.isDesktopCompact);' in COMPONENT,
    'subtree uses shared computed disclosure state':
        '*ngIf="isItemExpanded(item)"' in LAYOUT,
    'compact restoration regression test present':
        'does not announce hidden children as expanded in desktop compact mode' in TEST,
    'generic buttons cannot repaint material navigation on hover':
        GLOBAL_BUTTONS.count('button:not(.mat-mdc-button-base):not([mat-list-item])') == 6
        and 'button:not(.mat-mdc-button-base):hover' not in GLOBAL_BUTTONS,
    'shell hover foreground and surface use sidebar color tokens':
        '.nav-item:hover {\n  background: var(--shell-nav-hover-bg);\n  color: var(--shell-nav-text);' in CSS,
    'shell focus foreground and surface use sidebar color tokens':
        '.nav-item:focus-visible {\n  background: var(--shell-nav-hover-bg);\n  color: var(--shell-nav-text);' in CSS,
    'browser contrast regression covers branding, hover and focus':
        'toBeGreaterThanOrEqual(4.5)' in HOVER_E2E
        and 'brand-primary-hover' in HOVER_E2E
        and 'subopción seleccionada' in HOVER_E2E,

}

for label, passed in checks.items():
    print(f"{'PASS' if passed else 'FAIL'}: {label}")
if not all(checks.values()):
    raise SystemExit(1)
print(f'PASS: {len(checks)} static assertions (runtime unverified)')
