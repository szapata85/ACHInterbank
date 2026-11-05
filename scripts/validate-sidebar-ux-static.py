#!/usr/bin/env python3
"""Static guards for the global SPA navigation UX increment; not an Angular build."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
app = root / 'web/ach-interbank-ui/src/app'
layout = app / 'layout'
html = (layout / 'main-layout.component.html').read_text(encoding='utf-8')
ts = (layout / 'main-layout.component.ts').read_text(encoding='utf-8')
scss = (layout / 'main-layout.component.scss').read_text(encoding='utf-8')
icon = (app / 'shared/components/ui/ui-icon.component.ts').read_text(encoding='utf-8')
spec = (layout / 'main-layout.component.spec.ts').read_text(encoding='utf-8')
e2e = (root / 'web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts').read_text(encoding='utf-8')
nav = (app / 'core/services/navigation.service.ts').read_text(encoding='utf-8')
seed = (root / 'src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs').read_text(encoding='utf-8')
checks = {
    'Angular Material sidenav and toolbar kept': 'mat-sidenav' in html and 'mat-toolbar' in html,
    'Angular Material buttons for navigation': 'mat-button' in html and 'MatButtonModule' in ts,
    'No conflicting Material list-item titles': 'matListItemTitle' not in html and 'mat-list-item' not in html,
    'Search is accessible by label': 'aria-label="Buscar una opción en el menú"' in html,
    'Search is read only client-side filtered': 'get visibleMenuItems()' in ts and 'filterTree(this.menuItems)' in ts,
    'Search supports accent-insensitive matching': "normalize('NFD')" in ts and 'toLocaleLowerCase' in ts,
    'Search cannot insert fake menu options': "return children.length ? [{ ...item, children }] : []" in ts,
    'Search query empty state': 'No hay opciones que coincidan' in html,
    'Search result collapse accessible': 'searchCollapsedIds.has(item.id)' in ts,
    'Backend menu remains authority': 'this.menuItems = items;' in ts and 'getMenu()' in nav and 'sortMenu' in nav,
    'No forced home ordering or renamed source labels': 'sort(' not in ts and 'item.label' in html,
    'Expandable parent has one button': 'class="nav-item nav-parent"' in html and 'data-menu-toggle-id' in html,
    'Functional parent destination placed in submenu': html.index('class="nav-children"') < html.index('data-menu-destination-id'),
    'Floating arrow removed': 'nav-parent-destination' not in html and 'nav-parent-actions' not in html,
    'Functional parent classification maintained': 'hasFunctionalSurface(item.route)' in ts,
    'Hierarchical route and permission checks retained': 'activeItemIds' in ts and 'syncActiveNavigation' in ts,
    'Menu mobile close and escape kept': 'closeMobileNavigation()' in ts and 'onEscapeKey' in ts,
    'Compact desktop restores expansion': 'isDesktopCompact' in ts and 'expandedItems' in ts,
    'Dynamic sidebar branding background': 'var(--color-sidebar)' in scss and 'var(--color-on-sidebar)' in scss,
    'Material button label uses sidebar tokens': '--mdc-text-button-label-text-color: var(--shell-nav-text)' in scss,
    'Menu labels inherit active themed color': 'color: inherit;' in scss and '.nav-label' in scss,
    'No hardcoded foreground colors in nav stylesheet': not re.search(r'#[0-9a-fA-F]{3,8}\b|\brgb\s*\(',scss[:scss.index('.shell-main {')]),
    'Strong focus indication': ':focus-visible' in scss and 'outline: 2px solid var(--shell-nav-focus-ring)' in scss,
    'Menu icon fallback for unknown keys': 'fallbackIcon' in icon and 'fallbackIcon="folder"' in html and 'fallbackIcon="list"' in html,
    'No broad changes to global Material controls': 'MatListModule' not in ts,
    'Canonical seed labels untouched': 'FileSecurityLabel = "Seguridad de archivos"' in seed and 'Reconcile(home, HomeLabel, parentId: null, order: 10)' in seed,
    'Unit regression for live authorized menu': "renders the authorized dynamic source" in spec,
    'Unit regression for search and expand': 'filters authorized menu options ignoring accents' in spec and 'allows collapsing a filtered group' in spec,
    'Unit regression for functional-parent action': "puts group destinations inside their expanded section" in spec,
    'Playwright contrast verification preserved': 'toBeGreaterThanOrEqual(4.5)' in e2e,
    'Playwright mobile & search checks added': 'sidebar search is keyboard accessible' in e2e and 'mobile drawer keep controls accessible' in e2e,
}
failed = []
for name, result in checks.items():
    print(('PASS' if result else 'FAIL'), name)
    if not result:
        failed.append(name)
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks PASS')
sys.exit(1 if failed else 0)
