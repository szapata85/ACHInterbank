#!/usr/bin/env python3
"""Controles estáticos para la modernización UX de /customers; no sustituye ng build ni E2E."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
feat = root / 'src/app/features/customers'
read = lambda name: (feat / name).read_text(encoding='utf-8')
form_ts = read('components/customer-form.component.ts')
form_html = read('components/customer-form.component.html')
form_css = read('components/customer-form.component.scss')
list_ts = read('components/customers-list.component.ts')
list_html = read('components/customers-list.component.html')
list_css = read('components/customers-list.component.scss')
routes = read('customers-routing.module.ts')
api = (root / 'src/app/core/services/customers-api.service.ts').read_text(encoding='utf-8')
checks = [
    ('Ruta /customers conservada', "path: ''" in routes and 'CustomersListComponent' in routes),
    ('Rutas new y edit conservadas', "path: 'new'" in routes and "path: ':id/edit'" in routes),
    ('Ruta de terceros conservada', "path: ':id/third-parties'" in routes and "path: ':id/third-parties/:thirdPartyId'" in routes),
    ('Guardias y permisos conservados', routes.count('permissionGuard') >= 4 and "['CanManageAch']" in routes),
    ('Formulario usa Angular Material', all(x in form_html for x in ('<mat-card', '<mat-form-field', '<mat-select', 'matInput', '<mat-error', 'mat-flat-button'))),
    ('Listado usa Angular Material', all(x in list_html for x in ('<mat-card', '<mat-form-field', '<mat-select', 'matInput', 'mat-flat-button'))),
    ('Sin formularios nativos desalineados', all(not re.search(r'<select\b', h) and all('matInput' in tag for tag in re.findall(r'<input\b[^>]*>', h)) for h in (form_html, list_html))),
    ('Validación condicional PN/PJ', all(x in form_ts for x in ('updatePersonValidators()', 'companyName.setValidators', 'firstName.setValidators', 'lastName.setValidators'))),
    ('Documento requerido y sin solo espacios', 'Validators.pattern(/\\S/)' in form_ts),
    ('Cuenta requerida antes de guardar', 'if (!this.hasAccountNumbers)' in form_ts),
    ('Cuentas sin duplicados', 'this.accountNumbers.includes(accountNumber)' in form_ts),
    ('Cero inicial preservado', 'accountNumberInput.value' in form_ts and 'Number(accountNumber)' not in form_ts),
    ('Cuenta pendiente incluida en guardar', 'pendingAccount && !this.addAccountNumber()' in form_ts),
    ('Antidoble envío y estado de guardado', 'if (this.saving || this.loading || this.loadError)' in form_ts and 'this.saving = true;' in form_ts),
    ('Error de carga reintentable', 'loadCustomer()' in form_html and 'loadError' in form_ts),
    ('Error listado recuperable', '[error]="loadError"' in list_html and '(reintentar)="load()"' in list_html),
    ('Permisos visuales CRUD', "hasPermission('CanManageAch')" in list_ts and '*ngIf="canManage"' in list_html),
    ('Acceso a terceros mantenido', "'/customers', customer.id, 'third-parties'" in list_ts),
    ('Búsqueda por razón social', 'customer.companyName' in list_ts and 'nameValue' in list_ts),
    ('PN/PJ humanizados', "'Persona jurídica'" in list_ts and "'Persona natural'" in list_ts),
    ('Estados y mensajes en español', all(x in form_html+list_html for x in ('Guardar cambios', 'Buscar clientes', 'Cuentas asociadas', 'No pudimos'))),
    ('Tokens branding dinámico', all('--color-surface' in css and '--color-border' in css and '--color-primary' in css for css in (form_css, list_css))),
    ('Sin azules codificados en componentes', not re.search(r'#[0-9a-fA-F]{3,8}', form_css + list_css)),
    ('API mantiene métodos canónicos', all(x in api for x in ('getAll()', 'getById(', 'create(', 'update(', "this.basePath = 'customers'")) if "this.basePath = 'customers'" in api else all(x in api for x in ('getAll()', 'getById(', 'create(', 'update(', "basePath = 'customers'"))),
    ('Pruebas de regresión incluidas', (feat / 'components/customers-material-ux.component.spec.ts').is_file()),
]
for label, ok in checks:
    print(f"{'PASS' if ok else 'FAIL'} | {label}")
failed = sum(not ok for _, ok in checks)
print(f"TOTAL: {len(checks)-failed}/{len(checks)} PASS")
sys.exit(1 if failed else 0)
