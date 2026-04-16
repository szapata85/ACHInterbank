import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-vacio',
  standalone: true,
  template: `
    <div class="estado">
      <span class="icono material-symbols-outlined" aria-hidden="true">inbox</span>
      <p class="titulo">{{ titulo }}</p>
      <small>{{ mensaje }}</small>
      <small class="sugerencia">Puedes ajustar los filtros o crear un nuevo registro.</small>
    </div>
  `,
  styles: [`
    .estado{padding:1rem;border:1px dashed var(--color-border-strong);border-radius:var(--radius-md);text-align:center;color:var(--color-text-soft);display:grid;gap:.2rem;justify-items:center;animation:aparecer .18s ease}
    .icono{font-size:1.2rem;color:#64748b}
    .titulo{margin:0 0 .1rem;font-weight:700;color:var(--color-text-muted)}
    .sugerencia{margin-top:.15rem;color:#64748b}
    @keyframes aparecer{from{opacity:0;transform:translateY(2px)}to{opacity:1;transform:translateY(0)}}
  `]
})
export class UiEstadoVacioComponent {
  @Input() titulo = 'Sin resultados';
  @Input() mensaje = 'No se encontraron datos con los filtros actuales.';
}
