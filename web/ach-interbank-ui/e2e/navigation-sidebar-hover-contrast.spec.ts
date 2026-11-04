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

    const child = sidebar.locator('.nav-children a.nav-item').first();
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
