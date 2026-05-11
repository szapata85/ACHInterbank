import { createAchBadgeElement, createAchBooleanBadgeElement, createAchButtonElement } from './ach-response-renderers';

describe('ach-response-renderers', () => {
  it('creates badge and button', () => {
    const badge = createAchBadgeElement('Ok', 'estado-exitoso');
    const button = createAchButtonElement('Ver', 'ver');

    expect(badge.textContent).toBe('Ok');
    expect(button.dataset.action).toBe('ver');
  });

  it('creates contextual boolean badges', () => {
    const active = createAchBooleanBadgeElement(true, 'activo');
    const unknown = createAchBooleanBadgeElement(null, 'default');

    expect(active.className).toContain('estado-exitoso');
    expect(unknown.textContent).toBe('-');
  });
});
