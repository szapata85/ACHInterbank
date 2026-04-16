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
  styles: [`
    .estado{display:flex;align-items:center;gap:.6rem;padding:1rem;border:1px solid var(--color-border);border-radius:var(--radius-md);background:#f8fafc;color:var(--color-text-soft);animation:aparecer .18s ease}
    .spinner{width:16px;height:16px;border:2px solid #cbd5e1;border-top-color:var(--color-primary);border-radius:50%;animation:g .8s linear infinite}
    p{margin:0;font-weight:600}
    @keyframes g{to{transform:rotate(360deg)}}
    @keyframes aparecer{from{opacity:0;transform:translateY(2px)}to{opacity:1;transform:translateY(0)}}
  `]
})
export class UiEstadoCargaComponent {
  @Input() mensaje = 'Cargando información...';
}
