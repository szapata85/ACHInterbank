import { expect, test, type Page, type Request } from '@playwright/test';
import { assertNoFunctionalSpanglish } from './support/nacha-ui-language';

interface MockState {
  published: boolean;
  concurrencyConflict: boolean;
  fieldName: string;
  putCount: number;
  writes: string[];
  brandPrimary: string;
}

const workspacePath = '/nacha-config-admin/perfiles/10';

test.describe('NACHA Config UX6 cierre de experiencia', () => {
  let state: MockState;

  test.beforeEach(async ({ page }) => {
    state = {
      published: false,
      concurrencyConflict: false,
      fieldName: 'Número de cuenta',
      putCount: 0,
      writes: [],
      brandPrimary: '#123456'
    };
    await installMocks(page, state);
    await authenticate(page);
  });

  test('UX6-01 abre un perfil existente desde el listado', async ({ page }) => {
    await page.goto('/nacha-config-admin/perfiles');
    await expect(page.getByTestId('nacha-config-profiles-page')).toBeVisible();
    await expect(page.getByText('Perfil de pruebas', { exact: true })).toBeVisible();
    await page.locator('[data-action="ver"]').first().click();
    await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles\/10/);
    await expect(page.getByTestId('nacha-config-profile-workspace-page')).toBeVisible();
  });

  test('UX6-02 filtra servicios de forma contextual al cambiar de cámara', async ({ page }) => {
    await page.goto('/nacha-config-admin/perfiles');
    await page.getByTestId('create-profile-panel').locator('summary').click();

    const camera = page.getByRole('combobox', { name: 'Cámara' }).first();
    await camera.click();
    await page.getByRole('option', { name: /Red A/ }).click();
    const service = page.getByRole('combobox', { name: 'Servicio' }).first();
    await service.click();
    await expect(page.getByRole('option', { name: /Servicio de red A/ })).toBeVisible();
    await page.keyboard.press('Escape');

    await camera.click();
    await page.getByRole('option', { name: /Red B/ }).click();
    await service.click();
    await expect(page.getByRole('option', { name: /Servicio de red B/ })).toBeVisible();
    await expect(page.getByRole('option', { name: /Servicio de red A/ })).toHaveCount(0);
  });

  test('UX6-03 abre registro y campo por nombres funcionales', async ({ page }) => {
    await page.goto(workspacePath);
    await expect(page.getByTestId('nacha-config-record-list')).toContainText('Detalle de transacción');
    await expect(page.getByTestId('nacha-config-field-list')).toContainText('Número de cuenta');
    await expect(page.getByTestId('nacha-config-field-editor').getByRole('heading', { name: 'Editor del campo' })).toBeVisible();
  });

  test('UX6-04 cancelar edición de borrador no envía PUT', async ({ page }) => {
    await page.goto(workspacePath);
    const input = page.getByLabel('Nombre funcional');
    await input.fill('Nombre temporal');
    await page.getByRole('button', { name: 'Cancelar cambios' }).click();
    await expect(input).toHaveValue('Número de cuenta');
    expect(state.putCount).toBe(0);
  });

  test('UX6-05 guardar campo persiste y vuelve a presentar el valor', async ({ page }) => {
    await page.goto(workspacePath);
    await page.getByLabel('Nombre funcional').fill('Cuenta receptora');
    await page.getByRole('button', { name: 'Guardar campo' }).click();
    await expect.poll(() => state.putCount).toBe(1);
    await expect(page.getByLabel('Nombre funcional')).toHaveValue('Cuenta receptora');
  });

  test('UX6-06 conflicto de concurrencia se informa sin ocultarlo', async ({ page }) => {
    state.concurrencyConflict = true;
    await page.goto(workspacePath);
    await page.getByLabel('Nombre funcional').fill('Cambio concurrente');
    await page.getByRole('button', { name: 'Guardar campo' }).click();
    await expect(page.getByRole('alert')).toContainText('La configuración fue modificada por otro usuario');
  });

  test('UX6-07 una configuración publicada bloquea edición directa', async ({ page }) => {
    state.published = true;
    await page.goto(workspacePath);
    await expect(page.getByTestId('nacha-config-published-protection')).toContainText('Configuración protegida');
    await expect(page.getByRole('button', { name: 'Guardar campo' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Guardar borrador' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Clonar como borrador' })).toBeVisible();
  });

  test('UX6-08 validar desde listado muestra error localizable y claro', async ({ page }) => {
    await page.goto('/nacha-config-admin/perfiles');
    await page.locator('[data-action="validar"]').first().click();
    await expect(page.getByText('Última validación', { exact: true })).toBeVisible();
    await expect(page.getByText(/Posiciones superpuestas en Detalle de transacción/)).toBeVisible();
  });

  test('UX6-09 Cambios es consulta y no muta información', async ({ page }) => {
    await page.goto(workspacePath);
    const writesBefore = state.writes.length;
    await page.getByRole('tab', { name: 'Cambios' }).click();
    await expect(page.getByTestId('nacha-config-changes-panel')).toContainText('Posición inicial');
    expect(state.writes.length).toBe(writesBefore);
  });

  test('UX6-10 Historial es consulta y presenta auditoría humanizada', async ({ page }) => {
    await page.goto(workspacePath);
    const writesBefore = state.writes.length;
    await page.getByRole('tab', { name: 'Historial' }).click();
    const history = page.getByTestId('nacha-config-history-panel');
    await expect(history).toContainText('Campo actualizado');
    await expect(history).toContainText('operador.pruebas');
    expect(state.writes.length).toBe(writesBefore);
  });

  test('UX6-11 branding dinámico calcula contraste para primario oscuro y claro', async ({ page }) => {
    for (const brand of [
      { primary: '#123456', contrast: '#ffffff' },
      { primary: '#f5e9a8', contrast: '#0f172a' }
    ]) {
      state.brandPrimary = brand.primary;
      await page.goto(workspacePath);
      await expect.poll(() => rootToken(page, '--mat-sys-primary')).toBe(brand.primary);
      await expect.poll(() => rootToken(page, '--mat-sys-on-primary')).toBe(brand.contrast);
    }
  });

  test('UX6-12 workspace conserva navegación útil en móvil, tableta y escritorio', async ({ page }) => {
    for (const viewport of [
      { width: 390, height: 844 },
      { width: 820, height: 1180 },
      { width: 1440, height: 900 }
    ]) {
      await page.setViewportSize(viewport);
      await page.goto(workspacePath);
      await expect(page.getByTestId('nacha-config-profile-workspace-page')).toBeVisible();
      await expect(page.getByRole('tab', { name: 'Configuración' })).toBeVisible();
      await expect(page.getByRole('button', { name: 'Volver a perfiles' })).toBeVisible();
    }
  });

  test('UX6-13 rutas antiguas redirigen al workspace manteniendo contexto', async ({ page }) => {
    for (const oldRoute of ['records', 'variants-fields']) {
      await page.goto(`/nacha-config-admin/${oldRoute}?profileId=10&recordCode=6&fieldId=101`);
      await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles\/10/);
      await expect(page).toHaveURL(/recordCode=6/);
      await expect(page).toHaveURL(/fieldId=101/);
      await expect(page.getByTestId('nacha-config-profile-workspace-page')).toBeVisible();
    }
  });

  test('UX6-14 flujo feliz no genera errores de consola o página y mantiene textos funcionales en español', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', message => {
      if (message.type() === 'error') errors.push(message.text());
    });
    page.on('pageerror', error => errors.push(error.message));

    await page.goto(workspacePath);
    await assertNoFunctionalSpanglish(page.locator('body'));
    await page.getByRole('tab', { name: 'Probar' }).click();
    await page.getByRole('button', { name: 'Probar configuración' }).click();
    await expect(page.getByTestId('nacha-config-test-summary')).toContainText('Apto para publicar');
    await assertNoFunctionalSpanglish(page.locator('body'));
    await page.getByRole('tab', { name: 'Cambios' }).click();
    await assertNoFunctionalSpanglish(page.locator('body'));
    await page.getByRole('tab', { name: 'Historial' }).click();
    await assertNoFunctionalSpanglish(page.locator('body'));
    expect(errors).toEqual([]);
  });
});

async function installMocks(page: Page, state: MockState): Promise<void> {
  page.on('request', (request: Request) => {
    if (request.method() !== 'GET' && request.method() !== 'HEAD' && isNachaWrite(request.url())) {
      state.writes.push(`${request.method()} ${new URL(request.url()).pathname}`);
    }
  });

  await page.route(/\/auth\/refresh$/, async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: authPayload() }) }));
  await page.route(/\/navigation\/menu$/, async route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([{ id: 20, label: 'Configuración NACHA-M', route: '/nacha-config-admin/perfiles', children: [{ id: 21, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' }] }])
  }));
  await page.route(/\/api\/users\/branding$/, async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ buttonColor: state.brandPrimary }) }));

  await page.route(/\/api\/ach\/nacha\/config-profiles\/dashboard(?:\?.*)?$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify({
      productiveStatus: 'NO-GO', isOfficialModel: true, legacyDeprecated: false, profileCount: 1,
      publishedProfileCount: state.published ? 1 : 0, currentProfileCount: 1, layoutVariantCount: 1, fieldCount: 1,
      clearingHouses: ['RED_A', 'RED_B'], recordTypes: ['6'], warnings: []
    })
  }));
  await page.route(/\/api\/ach\/nacha\/config-profiles(?:\?.*)?$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify([profileReadModel(state)])
  }));

  await page.route(/\/nacha-config\/catalogos-operativos(?:\?.*)?$/, async route => {
    const url = new URL(route.request().url());
    const chamber = url.searchParams.get('camaraCode');
    const services = chamber === 'RED_A'
      ? [{ code: 'SERV_A', labelEs: 'Servicio de red A' }]
      : chamber === 'RED_B'
        ? [{ code: 'SERV_B', labelEs: 'Servicio de red B' }]
        : [{ code: 'SERV_A', labelEs: 'Servicio de red A' }, { code: 'SERV_B', labelEs: 'Servicio de red B' }];
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
      estados: [{ code: 'BORRADOR', labelEs: 'Borrador' }, { code: 'PUBLICADO', labelEs: 'Publicado' }],
      camaras: [{ code: 'RED_A', labelEs: 'Red A' }, { code: 'RED_B', labelEs: 'Red B' }],
      flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
      direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
      servicios: services,
      registros: [{ code: '6', labelEs: 'Detalle de transacción' }],
      tiposOrigen: [{ code: 'ENTIDAD', labelEs: 'Dato del sistema' }],
      tiposRegla: [{ code: 'REQUIRED', labelEs: 'Obligatorio' }]
    }) });
  });

  await page.route(/\/nacha-config\/perfiles\/10\/workspace(?:\?.*)?$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify(workspace(state))
  }));
  await page.route(/\/nacha-config\/perfiles\/10\/fields\/101\/editor(?:\?.*)?$/, async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(fieldEditor(state)) });
      return;
    }
    state.putCount += 1;
    if (state.concurrencyConflict) {
      await route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ errorCode: 'CONCURRENCY_CONFLICT', message: 'Conflicto', issues: [] }) });
      return;
    }
    const body = route.request().postDataJSON() as { fieldNameEs?: string };
    state.fieldName = body.fieldNameEs?.trim() || state.fieldName;
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });

  await page.route(/\/nacha-config\/perfiles\/10\/validar$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify({
      profileId: 10, isValid: false, erroresBloqueantes: 1, advertencias: 0, resumen: 'Requiere correcciones',
      issues: [{ severidad: 'ERROR', codigo: 'FIELD_OVERLAP', mensaje: 'Posiciones superpuestas en Detalle de transacción.' }]
    })
  }));
  await page.route(/\/nacha-config\/perfiles\/10\/probar$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify(testResult(state))
  }));
  await page.route(/\/nacha-config\/perfiles\/10\/cambios$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify(changeReview(state))
  }));
  await page.route(/\/nacha-config\/perfiles\/10\/historial$/, async route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify([{ id: 1, changedAtUtc: '2026-10-11T18:00:00Z', changedBy: 'operador.pruebas', changeType: 'FIELD_EDITOR_UPDATE', entityName: 'CfgLayoutField', actionNameEs: 'Campo actualizado', elementNameEs: state.fieldName, version: '1.0', summaryEs: `Campo actualizado en ${state.fieldName}.`, detailEs: ['Posición inicial: 5 → 6'] }])
  }));

  await page.route(/\/nacha-config\/perfiles(?:\?.*)?$/, async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ id: 10, profileCode: 'PERFIL_PRUEBAS', nombreEs: 'Perfil de pruebas', estado: state.published ? 'PUBLICADO' : 'BORRADOR' }]) });
      return;
    }
    await route.fulfill({ status: 405, contentType: 'application/json', body: '{}' });
  });
}

async function authenticate(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'ux6.pruebas', name: 'Usuario de pruebas', uid: 'ux6-tests',
    role: ['Admin', 'ACH.Operator'], permission: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600, iat: Math.floor(Date.now() / 1000)
  });
  await page.addInitScript(accessToken => window.sessionStorage.setItem('ach.interbank.access_token', accessToken), token);
}

function profileReadModel(state: MockState) {
  return {
    profileId: 10, profileCode: 'PERFIL_PRUEBAS', profileName: 'Perfil de pruebas', clearingHouseCode: 'RED_A',
    flowType: 'ORIGINAL', status: state.published ? 'PUBLICADO' : 'BORRADOR', version: '1.0', isPublished: state.published,
    isCurrent: true, effectiveFrom: '2026-01-01', effectiveTo: null, layoutVariantCount: 1, fieldCount: 1,
    recordTypes: ['6'], isOfficialModel: true, legacyDeprecated: false
  };
}

function workspace(state: MockState) {
  return {
    id: 10, profileCode: 'PERFIL_PRUEBAS', nombreEs: 'Perfil de pruebas', descripcion: 'Configuración para pruebas UX6',
    estado: state.published ? 'PUBLICADO' : 'BORRADOR', estadoNombre: state.published ? 'Publicado' : 'Borrador', isEditable: !state.published,
    camara: 'RED_A', camaraNombre: 'Red A', flujo: 'ORIGINAL', flujoNombre: 'Original', direccion: 'SALIDA', direccionNombre: 'Salida',
    servicio: 'SERV_A', servicioNombre: 'Servicio de red A', versionMajor: 1, versionMinor: 0, contextPriority: 100,
    effectiveFrom: '2026-01-01', effectiveTo: null, rowVersion: 'cm93',
    records: [{ id: 1, recordCode: '6', recordNameEs: 'Detalle de transacción', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC', sourceStrategyName: 'Estático' }],
    variantes: [{ id: 20, recordCode: '6', variantCode: 'R6_BASE', nombreEs: 'Detalle base', priority: 1, isDefaultForRecord: true, totalLength: 106,
      fields: [{ id: 101, fieldCode: 'ACCOUNT', fieldNameEs: state.fieldName, startPosition: 5, length: 17, padChar: ' ', justification: 'L', sourceDefinitionId: 901, sourceType: 'ENTIDAD', sourceTypeName: 'Dato del sistema', isEnabled: true, reglas: [] }] }]
  };
}

function fieldEditor(state: MockState) {
  return {
    profileId: 10, fieldId: 101, isEditable: !state.published, rowVersion: 'cm93', sourceReferenceCount: 1,
    sourceTypes: [{ code: 'ENTIDAD', labelEs: 'Dato del sistema', catalogLabelEs: 'Dato autorizado', usesFixedValue: false, requiresDefinitionSelection: true }],
    sourceOptions: [{ sourceDefinitionId: 901, sourceTypeCode: 'ENTIDAD', labelEs: 'Dato del sistema → Cuenta receptora' }],
    alignments: [{ code: 'L', labelEs: 'Alinear a la izquierda' }, { code: 'R', labelEs: 'Alinear a la derecha' }],
    formats: [], severities: [{ code: 'ERROR', labelEs: 'Error bloqueante' }],
    field: {
      id: 101, fieldCode: 'ACCOUNT', fieldNameEs: state.fieldName, startPosition: 5, length: 17, padChar: ' ', justification: 'L', formatMask: null,
      sourceDefinitionId: 901, sourceType: 'ENTIDAD', sourceTypeName: 'Dato del sistema', propertyPath: 'Beneficiary.Account.Number', entityName: 'Transferencia',
      sqlObjectName: null, externalCatalogCode: null, expressionDsl: null, fallbackPolicyJson: null, isEnabled: true,
      reglas: [{ id: 701, ruleType: 'REQUIRED', ruleTypeName: 'Obligatorio', errorCode: 'REQ_ACCOUNT', errorMessageEs: 'La cuenta es obligatoria', severity: 'ERROR', severityName: 'Error bloqueante', ruleCode: 'ACCOUNT_REQUIRED', conditionDsl: null, ruleConfigJson: null, order: 1, isEnabled: true }]
    }
  };
}

function testResult(state: MockState) {
  return {
    profileId: 10, processDateUtc: '2026-10-11T00:00:00Z', canPublish: true, resultLabelEs: 'Apto para publicar', erroresBloqueantes: 0, advertencias: 0, issues: [],
    selection: { success: true, summaryEs: 'La configuración coincide con el contexto seleccionado.', warningsEs: [], technicalTrace: [] },
    records: [{ variantId: 20, recordCode: '6', recordNameEs: 'Detalle de transacción', variantCode: 'R6_BASE', variantNameEs: 'Detalle base', totalLength: 106, isDefaultForRecord: true, contentPreviewAvailable: true, renderedRecord: '1234567890'.padEnd(106, ' '), fields: [{ fieldId: 101, fieldNameEs: state.fieldName, startPosition: 5, endPosition: 21, length: 17, isEnabled: true, isRequired: true, sourceSummaryEs: 'Dato del sistema → Cuenta receptora', formatSummaryEs: 'Sin formato especial', sampleValue: '1234567890', relevantRulesEs: ['Contenido: valor requerido'] }] }]
  };
}

function changeReview(state: MockState) {
  return {
    profileId: 10, baseProfileId: 9, hasComparisonBase: true, isEditable: !state.published, rowVersion: 'cm93', currentVersion: '1.0', nextVersion: '1.1',
    camaraNombreEs: 'Red A', flujoNombreEs: 'Original', direccionNombreEs: 'Salida', servicioNombreEs: 'Servicio de red A', effectiveFrom: '2026-01-01', effectiveTo: null,
    changedFieldsCount: 1, changeCount: 1, summaryEs: 'Un cambio real.', changes: [{ scope: 'FIELD', changeKind: 'MODIFIED', elementNameEs: state.fieldName, recordCode: '6', recordNameEs: 'Detalle de transacción', fieldId: 101, fieldNameEs: state.fieldName, propertyNameEs: 'Posición inicial', beforeValueEs: '5', afterValueEs: '6', summaryEs: 'Posición inicial: 5 → 6' }]
  };
}

function isNachaWrite(url: string): boolean {
  const path = new URL(url).pathname;
  return path.startsWith('/nacha-config/') && !path.endsWith('/probar') && !path.endsWith('/validar');
}

function authPayload() {
  return { token: createUnsignedJwt({ unique_name: 'ux6.pruebas', role: ['Admin', 'ACH.Operator'], permission: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'], exp: Math.floor(Date.now() / 1000) + 3600 }), username: 'ux6.pruebas', fullName: 'Usuario de pruebas', roles: ['Admin', 'ACH.Operator'], permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'] };
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

async function rootToken(page: Page, token: string): Promise<string> {
  return page.evaluate(name => getComputedStyle(document.documentElement).getPropertyValue(name).trim(), token);
}
