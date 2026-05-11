import { formatAchBoolean } from './ach-response-formatters';

export function createAchBadgeElement(text: string, cssClass: string): HTMLElement {
  const span = document.createElement('span');
  span.classList.add('estado-pill');
  if (cssClass?.trim()) span.classList.add(cssClass);
  span.textContent = text;
  return span;
}

export function createAchButtonElement(text: string, action: string): HTMLButtonElement {
  const button = document.createElement('button');
  button.type = 'button';
  button.classList.add('btn', 'btn-outline', 'btn-grid');
  button.dataset.action = action;
  button.textContent = text;
  return button;
}

export function createAchBooleanBadgeElement(
  value: boolean | null | undefined,
  context: 'activo' | 'requiereCausal' | 'permiteNotificacion' | 'default' = 'default'
): HTMLElement {
  if (value === null || value === undefined) {
    return createAchBadgeElement('-', 'estado-advertencia');
  }

  const text = formatAchBoolean(value);

  if (context === 'activo') {
    return createAchBadgeElement(text, value ? 'estado-exitoso' : 'estado-neutro');
  }

  if (context === 'requiereCausal') {
    return createAchBadgeElement(text, value ? 'estado-advertencia' : 'estado-neutro');
  }

  if (context === 'permiteNotificacion') {
    return createAchBadgeElement(text, value ? 'estado-exitoso' : 'estado-advertencia');
  }

  return createAchBadgeElement(text, value ? 'estado-exitoso' : 'estado-neutro');
}
