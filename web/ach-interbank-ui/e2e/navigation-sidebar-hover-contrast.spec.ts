import { expect, Locator, Page, test } from '@playwright/test';

/**
 * I06 visual contrast regression. Execute with an authorized local QA SPA/API,
 * using ACH_USER and ACH_PASS; it never mocks permissions or server menus.
 * Material list-item buttons must not inherit global primary-button hover styling.
 */
test('sidebar labels retain WCAG AA contrast on hover, focus and expanded/active states', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await login(page);

  const sidebar = page.locator('mat-sidenav.primary-sidenav');
  const parent = sidebar.locator('button.nav-parent').first();
  await expect(parent).toBeVisible();

  for (const theme of [
    { name: 'oscuro', background: '#0f172a', foreground: '#ffffff' },
    { name: 'claro', background: '#ffffff', foreground: '#0f172a' }
  ]) {
    // The primary button can be very light while the sidebar is dark (or vice versa).
    // It must never become the nav row's hover background.
    await page.evaluate(({ background, foreground }) => {
      const root = document.documentElement;
      root.style.setProperty('--brand-sidebar-background', background);
      root.style.setProperty('--brand-sidebar-on-background', foreground);
      root.style.setProperty('--brand-primary-hover', '#e2e8f0');
    }, theme);

    await parent.hover();
    await expectLabelContrast(parent, `grupo hover tema ${theme.name}`);
    if ((await parent.getAttribute('aria-expanded')) !== 'true') {
      await parent.click();
    }
    await expect(parent).toHaveAttribute('aria-expanded', 'true');
    await expectLabelContrast(parent, `grupo expandido tema ${theme.name}`);

    const child = sidebar.locator('.nav-children a.nav-link').first();
    await expect(child).toBeVisible();
    await child.hover();
    await expectLabelContrast(child, `subopción hover tema ${theme.name}`);
    await child.focus();
    await expectLabelContrast(child, `subopción con foco tema ${theme.name}`);
    await child.click();
    await expect(child).toHaveClass(/\bactive\b/);
    await expectLabelContrast(child, `subopción seleccionada tema ${theme.name}`);
    await page.screenshot({ path: testInfo.outputPath(`sidebar-hover-${theme.name}.png`) });
  }
});

async function login(page: Page): Promise<void> {
  const username = process.env['ACH_USER'];
  const password = process.env['ACH_PASS'];
  if (!username || !password) {
    throw new Error('Se requiere ACH_USER y ACH_PASS de un entorno de QA autorizado');
  }
  await page.goto('/login');
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/auth/login') && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar', exact: true }).click()
  ]);
  await expect(page.locator('mat-sidenav.primary-sidenav')).toBeVisible();
}

async function expectLabelContrast(item: Locator, message: string): Promise<void> {
  const contrast = await item.evaluate((element) => {
    type Rgba = [number, number, number, number];
    const parse = (value: string): Rgba => {
      if (value === 'transparent') return [0, 0, 0, 0];
      const srgb = value.match(/^color\(srgb\s+([^)]*)\)$/i);
      if (srgb) {
        const [channels, alpha] = srgb[1].split('/').map((part) => part.trim());
        const parts = channels.split(/\s+/).map(Number);
        return [parts[0] * 255, parts[1] * 255, parts[2] * 255, alpha ? Number(alpha) : 1];
      }
      const rgb = value.match(/^rgba?\(([^)]*)\)$/i);
      if (rgb) {
        const parts = rgb[1].replace('/', ',').split(/[,\s]+/).filter(Boolean).map(Number);
        return [parts[0], parts[1], parts[2], parts[3] ?? 1];
      }
      throw new Error(`Color no soportado por el test visual: ${value}`);
    };
    const composite = (base: Rgba, layer: Rgba): Rgba => {
      const a = layer[3];
      return [base[0] * (1 - a) + layer[0] * a, base[1] * (1 - a) + layer[1] * a, base[2] * (1 - a) + layer[2] * a, 1];
    };
    const elements: Element[] = [];
    for (let node: Element | null = element; node; node = node.parentElement) elements.push(node);
    const background = elements.reverse().reduce<Rgba>((current, node) =>
      composite(current, parse(getComputedStyle(node).backgroundColor)), [255, 255, 255, 1]);
    const textNode = element.querySelector('.nav-label') ?? element;
    const text = parse(getComputedStyle(textNode).color);
    const luminance = (color: Rgba): number => {
      const channels = color.slice(0, 3).map((n) => {
        const c = n / 255;
        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
      });
      return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
    };
    const foreground = composite(background, text);
    const a = luminance(foreground);
    const b = luminance(background);
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
  expect(contrast, `${message}: contraste real ${contrast.toFixed(2)}:1`).toBeGreaterThanOrEqual(4.5);
}

test('sidebar search is keyboard accessible and never creates unauthorized options', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 900 });
  await login(page);

  const sidebar = page.locator('mat-sidenav.primary-sidenav');
  const initialMenuIds = await sidebar.locator('[data-menu-item-id]').evaluateAll((nodes) =>
    nodes.map((node) => node.getAttribute('data-menu-item-id'))
  );
  const search = sidebar.getByRole('searchbox', { name: 'Buscar una opción en el menú' });
  await search.fill('zzzz_ninguna_opcion');
  await expect(sidebar.getByText('No hay opciones que coincidan con tu búsqueda.')).toBeVisible();
  await expect(sidebar.locator('[data-menu-item-id]')).toHaveCount(0);
  await expect(search).toBeFocused();

  await sidebar.getByRole('button', { name: 'Limpiar búsqueda del menú' }).click();
  const restoredMenuIds = await sidebar.locator('[data-menu-item-id]').evaluateAll((nodes) =>
    nodes.map((node) => node.getAttribute('data-menu-item-id'))
  );
  expect(restoredMenuIds).toEqual(initialMenuIds);
});

test('compact navigation and mobile drawer keep controls accessible', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 900 });
  await login(page);

  const sidebar = page.locator('mat-sidenav.primary-sidenav');
  await expect(sidebar.locator('.nav-parent-destination')).toHaveCount(0);
  const toggle = page.getByRole('button', { name: 'Contraer navegación' });
  await toggle.click();
  await expect(sidebar).toHaveClass(/compact/);
  await expect(sidebar.getByRole('searchbox', { name: 'Buscar una opción en el menú' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Expandir navegación' }).click();
  await expect(sidebar.getByRole('searchbox', { name: 'Buscar una opción en el menú' })).toBeVisible();

  await page.setViewportSize({ width: 390, height: 844 });
  await expect(sidebar).not.toBeVisible();
  await page.getByRole('button', { name: 'Abrir navegación' }).click();
  await expect(sidebar).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(sidebar).not.toBeVisible();
  await expect(page.getByRole('button', { name: 'Abrir navegación' })).toBeFocused();
});

// Prevent regression from .mdc-button__label: Angular Material wraps ng-content,
// so styling the button host as flex does NOT lay out icon/label/chevron directly.
test('sidebar entries render as compact horizontal rows with no icon-label stacking', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 900 });
  await login(page);
  const sidebar = page.locator('mat-sidenav.primary-sidenav');
  const rows = sidebar.locator('.menu-list > .nav-node > .nav-item');
  await expect(rows.first()).toBeVisible();

  const geometry = await rows.evaluateAll((nodes) => nodes.map((node) => {
    const row = node as HTMLElement;
    const content = row.querySelector('.nav-item-content');
    const icon = content?.querySelector('.nav-icon');
    const label = content?.querySelector('.nav-label');
    const arrow = content?.querySelector('.expand-icon');
    const position = (el: Element | null | undefined) => {
      if (!el) return null;
      const rect = el.getBoundingClientRect();
      return { left: rect.left, right: rect.right, top: rect.top,
        bottom: rect.bottom, midY: (rect.top + rect.bottom) / 2 };
    };
    return { row: position(row), content: position(content), icon: position(icon),
      label: position(label), arrow: position(arrow),
      materialLabelWidth: row.querySelector('.mdc-button__label')?.getBoundingClientRect().width ?? 0 };
  }));

  expect(geometry.length).toBeGreaterThan(2);
  for (const [index, item] of geometry.entries()) {
    expect(item.content, `fila ${index}: contenedor horizontal`).not.toBeNull();
    expect(item.icon, `fila ${index}: ícono`).not.toBeNull();
    expect(item.label, `fila ${index}: etiqueta`).not.toBeNull();
    expect(item.row, `fila ${index}: fila`).not.toBeNull();
    if (!item.row || !item.icon || !item.label || !item.content) continue;
    expect(item.row.bottom - item.row.top, `fila ${index}: altura compacta`).toBeLessThan(78);
    expect(item.label.left, `fila ${index}: texto a la derecha del ícono`).toBeGreaterThan(item.icon.right);
    expect(Math.abs(item.label.midY - item.icon.midY), `fila ${index}: alineación vertical`).toBeLessThan(9);
    expect(item.materialLabelWidth, `fila ${index}: ancho Material`).toBeGreaterThan(180);
    expect(item.content.right, `fila ${index}: sin desbordar`).toBeLessThanOrEqual(item.row.right + 1);
    if (item.arrow) {
      expect(item.arrow.left, `fila ${index}: flecha a la derecha del texto`).toBeGreaterThan(item.label.right - 1);
      expect(Math.abs(item.arrow.midY - item.icon.midY), `fila ${index}: flecha alineada`).toBeLessThan(9);
    }
  }

  // Compact mode keeps icons centered, without exposing off-row labels.
  await page.getByRole('button', { name: 'Contraer navegación' }).click();
  const compactRows = sidebar.locator('.menu-list > .nav-node > .nav-item');
  const positions = await compactRows.first().evaluate((node) => {
    const row = node.getBoundingClientRect();
    const icon = node.querySelector('.nav-icon')!.getBoundingClientRect();
    return { delta: Math.abs((row.left + row.right) / 2 - (icon.left + icon.right) / 2),
      height: row.height };
  });
  expect(positions.delta).toBeLessThan(5);
  expect(positions.height).toBeLessThan(65);
});
