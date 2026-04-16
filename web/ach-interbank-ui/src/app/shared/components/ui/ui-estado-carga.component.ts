import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-carga',
  standalone: true,
  template: `
    <div class="estado">
      <span class="spinner" aria-hidden="true"></span>
      <p>{{ mensaje }}</p>
    </div>
  `,
  styles: [`.estado{display:flex;align-items:center;gap:.6rem;padding:1rem;color:var(--color-text-soft)}.spinner{width:16px;height:16px;border:2px solid #cbd5e1;border-top-color:var(--color-primary);border-radius:50%;animation:g .8s linear infinite}@keyframes g{to{transform:rotate(360deg)}}`]
})
export class UiEstadoCargaComponent {
  @Input() mensaje = 'Cargando información...';
}
